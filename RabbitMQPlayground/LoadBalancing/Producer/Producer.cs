using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitMQPlayground.LoadBalancing
{
    public class Producer : IProducer
    {

        private IProducerConfiguration _producerConfiguration;
        private readonly IModel _channel;
        private readonly ISerializer _serializer;
        private readonly string _workResultQueue;
        private readonly Dictionary<string, TaskCompletionSource<IWorkResult>> _commandResults;

        public Producer(IProducerConfiguration producerConfiguration, ISerializer serializer)
        {
            Id = Guid.NewGuid();

            _producerConfiguration = producerConfiguration;
            _channel = _producerConfiguration.Connection.CreateModel();
            _serializer = serializer;

            _commandResults = new Dictionary<string, TaskCompletionSource<IWorkResult>>();

            _workResultQueue = CreateCommandResultHandlingQueue();

            _channel.QueueDeclare(queue: _producerConfiguration.BrokerWorkloadQueue, 
                                  durable: false, 
                                  exclusive: false, 
                                  autoDelete: false, 
                                  arguments: null);


        }

        public Guid Id { get; }

        public void Dispose()
        {
            _channel.Dispose();
        }

        private string CreateCommandResultHandlingQueue()
        {
            var queueName = _channel.QueueDeclare(exclusive: true, autoDelete: true).QueueName;

            var resultHandler = new EventingBasicConsumer(_channel);

            resultHandler.Received += (model, arg) =>
            {
                var body = arg.Body;
                var response = Encoding.UTF8.GetString(body);
                var correlationId = arg.BasicProperties.CorrelationId;

                if (_commandResults.ContainsKey(correlationId))
                {
                    var task = _commandResults[correlationId];
                    var type = Type.GetType(arg.BasicProperties.Type);
                    var message = (IWorkResult)_serializer.Deserialize(body, type);

                    task.SetResult(message);

                    _commandResults.Remove(correlationId);

                }
            };

            _channel.BasicConsume(
               consumer: resultHandler,
               queue: queueName,
               autoAck: true);

            return queueName;
        }

        public Task<TResult> SendWork<TArgument, TResult>(IWork<TArgument,TResult> work) where TResult : class, IWorkResult
        {
            var task = new TaskCompletionSource<IWorkResult>();
            var properties = _channel.CreateBasicProperties();
            var correlationId = Guid.NewGuid().ToString();

            var body = _serializer.Serialize(work);

            properties.ContentType = _serializer.ContentMIMEType;
            properties.ContentEncoding = _serializer.ContentEncoding;
            properties.Type = work.GetType().ToString();
            properties.CorrelationId = correlationId;
            properties.ReplyTo = _workResultQueue;

            _commandResults.Add(correlationId, task);

            var cancel = new CancellationTokenSource(_producerConfiguration.CommandTimeout);
            cancel.Token.Register(() => task.TrySetCanceled(), false);

            _channel.BasicPublish(
                exchange: string.Empty,
                routingKey: _producerConfiguration.BrokerWorkloadQueue,
                basicProperties: properties,
                mandatory: true,
                body: body);

            return task.Task.ContinueWith(t =>
            {
                if (t.Result.IsError)
                {
                    throw new WorkHandlingFailureException(t.Result as IWorkErrorResult);
                }

                return t.Result as TResult;

            }, cancel.Token);
        }
    }
}
