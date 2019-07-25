using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitMQPlayground.LoadBalancing
{
    public class Broker : IBroker
    {
        private readonly IBrokerConfiguration _configuration;
        private readonly ISerializer _serializer;
        private readonly IModel _channel;
        private readonly BlockingCollection<BrokerWorkload> _workloads;
        private readonly BlockingCollection<IWorkerDescriptor> _workers;
        private CancellationTokenSource _cancel;
        private readonly Task _workProc;

        public Broker(IBrokerConfiguration configuration, ISerializer serializer)
        {
            _configuration = configuration;
            _serializer = serializer;

            _workloads = new BlockingCollection<BrokerWorkload>();
            _workers = new BlockingCollection<IWorkerDescriptor>();

            _channel = _configuration.Connection.CreateModel();

            Id = Guid.NewGuid();

            _channel.QueueDeclare(queue: _configuration.WorkerRegistrationQueue,
                      durable: false,
                      exclusive: false,
                      autoDelete: false,
                      arguments: null);

            _channel.QueueDeclare(queue: _configuration.WorkloadQueue,
                     durable: false,
                     exclusive: false,
                     autoDelete: false,
                     arguments: null);

            var workHandler = new EventingBasicConsumer(_channel);
            var workerRegistrationHandler = new EventingBasicConsumer(_channel);

            workHandler.Received += (model, arg) =>
            {
                var body = arg.Body;
                var correlationId = arg.BasicProperties.CorrelationId;
                var producerId = arg.BasicProperties.ReplyTo;
                var workloadType = arg.BasicProperties.Type;

                var payload = new Payload()
                {
                    WorkLoad = body,
                    WorkLoadType = Type.GetType(workloadType)
                };

                var workload = new BrokerWorkload(payload, producerId, correlationId);

                _workloads.Add(workload);

            };

            workerRegistrationHandler.Received += (model, arg) =>
            {
                var body = arg.Body;
                var type = Type.GetType(arg.BasicProperties.Type);

                var worker = (IWorkerDescriptor)_serializer.Deserialize(body, type);

                _workers.Add(worker);
            };


            _channel.BasicConsume(
               consumer: workHandler,
               queue: _configuration.WorkloadQueue,
               autoAck: true);


            _channel.BasicConsume(
               consumer: workerRegistrationHandler,
               queue: _configuration.WorkerRegistrationQueue,
               autoAck: true);

            _cancel = new CancellationTokenSource();
            _workProc = Task.Run(DoWork, _cancel.Token);
        }

        public Guid Id { get; private set; }

        public void Dispose()
        {
            _cancel.Cancel();
        }

        public void DoWork()
        {
            foreach (var work in _workloads.GetConsumingEnumerable(_cancel.Token))
            {
                foreach (var worker in _workers.GetConsumingEnumerable(_cancel.Token))
                {

                    var properties = _channel.CreateBasicProperties();
                    properties.CorrelationId = work.CorrelationId;
                    properties.ContentType = _serializer.ContentMIMEType;
                    properties.ContentEncoding = _serializer.ContentEncoding;
                    properties.ReplyTo = work.ProducerId;
                    properties.Type = typeof(Payload).ToString();

                    var replyMessage = _serializer.Serialize(work.Payload);

                    _channel.BasicPublish(
                        exchange: string.Empty,
                        routingKey: worker.Id,
                        mandatory: true,
                        basicProperties: properties,
                        body: replyMessage);

                    break;
                }
            }
        }
    }
}
