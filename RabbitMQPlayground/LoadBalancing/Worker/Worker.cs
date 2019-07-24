using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitMQPlayground.LoadBalancing
{
    public class Worker<TArgument, TResult> : IWorker<TArgument, TResult> where TResult : class, IWorkResult
    {
        private readonly IWorkerConfiguration _configuration;
        private readonly ISerializer _serializer;
        private readonly IModel _channel;
        private readonly BlockingCollection<IScheduledWorkload<TArgument, TResult>> _workloads;
        private readonly CancellationTokenSource _cancel;
        private readonly Task _workProc;
        private readonly string _workerQueue;
        private readonly string _workerQueueName;

        private readonly IWorkerDescriptor _self;

        public Worker(IWorkerConfiguration configuration, ISerializer serializer)
        {
            Id = Guid.NewGuid();
            _configuration = configuration;
            _serializer = serializer;

            _channel = _configuration.Connection.CreateModel();
            _workloads = new BlockingCollection<IScheduledWorkload<TArgument, TResult>>();

            _self = new WorkerDescriptor() { Id = Id.ToString() };

            _cancel = new CancellationTokenSource();

            _workProc = Task.Run(DoWork, _cancel.Token);

            _channel.QueueDeclare(queue: _configuration.BrokerWorkerRegistrationQueue,
                                  durable: false,
                                  exclusive: false,
                                  autoDelete: false,
                                  arguments: null);

            _workerQueueName = _channel.QueueDeclare(queue: Id.ToString(),
                                   durable: false,
                                   exclusive: true,
                                   autoDelete: true,
                                   arguments: null).QueueName;

            var scheduler = new EventingBasicConsumer(_channel);

            scheduler.Received += (model, arg) =>
            {
                var body = arg.Body;
                var correlationId = arg.BasicProperties.CorrelationId;
                var producerId = arg.BasicProperties.ReplyTo;

                try
                {

                    var payload = _serializer.Deserialize<Payload>(body);
                    var workload = (IWorkload<TArgument, TResult>)_serializer.Deserialize(payload.WorkLoad, payload.WorkLoadType);
                    var producerDescriptor = new ProducerDescriptor(producerId, correlationId);
                    var scheduledWorkload = new ScheduledWorkload<TArgument, TResult>(workload, producerDescriptor);

                    _channel.BasicAck(deliveryTag: arg.DeliveryTag, multiple: false);

                    Schedule(scheduledWorkload);

                }

                catch (Exception ex)
                {
                    var replyProperties = _channel.CreateBasicProperties();
                    replyProperties.CorrelationId = arg.BasicProperties.CorrelationId;
                    replyProperties.ContentType = _serializer.ContentMIMEType;
                    replyProperties.ContentEncoding = _serializer.ContentEncoding;

                    _channel.BasicReject(deliveryTag: arg.DeliveryTag, requeue: false);

                    var error = new WorkErrorResult()
                    {
                        ErrorMessage = "Unable to handle the work"
                    };

                    replyProperties.Type = typeof(WorkErrorResult).ToString();

                    var replyMessage = _serializer.Serialize(error);

                    _channel.BasicPublish(
                        exchange: string.Empty,
                        routingKey: arg.BasicProperties.ReplyTo,
                        mandatory: true,
                        basicProperties: replyProperties,
                        body: replyMessage);

                }

            };

            _channel.BasicConsume(
               consumer: scheduler,
               queue: _workerQueueName,
               autoAck: false);


            SignalReadyForWork();

        }

        public Guid Id { get; private set; }

        public void Schedule(IScheduledWorkload<TArgument, TResult> workload)
        {
            _workloads.Add(workload);
        }

        public void SignalReadyForWork()
        {
            var properties = _channel.CreateBasicProperties();
            properties.ContentType = _serializer.ContentMIMEType;
            properties.ContentEncoding = _serializer.ContentEncoding;
            properties.Type = typeof(WorkerDescriptor).ToString();

            var message = _serializer.Serialize(_self);

            _channel.BasicPublish(
                     exchange: string.Empty,
                     routingKey: _configuration.BrokerWorkerRegistrationQueue,
                     mandatory: true,
                     basicProperties: properties,
                     body: message);
        }

        public async Task DoWork()
        {
            foreach (var workload in _workloads.GetConsumingEnumerable(_cancel.Token))
            {
                var result = await Handle(workload.Workload.Work, workload.Workload.Argument);

                var replyProperties = _channel.CreateBasicProperties();
                replyProperties.CorrelationId = workload.ProducerDescriptor.CorrelationId;
                replyProperties.ContentType = _serializer.ContentMIMEType;
                replyProperties.ContentEncoding = _serializer.ContentEncoding;

                replyProperties.Type = typeof(TResult).ToString();

                var replyMessage = _serializer.Serialize(result);

                _channel.BasicPublish(
                      exchange: string.Empty,
                      routingKey: workload.ProducerDescriptor.Id,
                      mandatory: true,
                      basicProperties: replyProperties,
                      body: replyMessage);


                SignalReadyForWork();
            }
        }

        public void Dispose()
        {
            _cancel.Cancel();
        }

        public async Task<TResult> Handle(IWork<TArgument, TResult> work, TArgument argument)
        {
            return await work.Execute(argument);
        }


    }
}
