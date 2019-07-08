using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQPlayground.Routing
{
    public class Bus : IBus
    {
        class SubscriberDesriptor
        {
            public SubscriberDesriptor(ISubscription subscription, EventingBasicConsumer consumer, string queueName)
            {
                Subscription = subscription;
                Consumer = consumer;
                QueueName = queueName;
            }

            public ISubscription Subscription { get; }
            public EventingBasicConsumer Consumer { get; }
            public string QueueName { get; }
        }

        private readonly ConnectionFactory _factory;
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly EventingBasicConsumer _consumer;
        private readonly List<SubscriberDesriptor> _subscriber;

        public Bus(string host, ISerializer serializer)
        {
            Id = Guid.NewGuid();

            _serializer = serializer;
            _subscriber = new List<SubscriberDesriptor>();

            _factory = new ConnectionFactory() { HostName = host };
            _connection = _factory.CreateConnection();
            _channel = _connection.CreateModel();

        }

        public Guid Id { get; }

        private readonly ISerializer _serializer;

        public void Dispose()
        {
            _connection.Dispose();
            _channel.Dispose();
        }

        public void Emit(IEvent @event)
        {
            throw new NotImplementedException();
        }

        public void Subscribe<TEvent>(ISubscription<TEvent> handler)
        {
      
            _channel.ExchangeDeclare(exchange: handler.Exchange, type: "topic");

            var queueName = _channel.QueueDeclare().QueueName;

            var consumer = new EventingBasicConsumer(_channel);

            _consumer.Received += (model, arg) =>
            {
                var body = arg.Body;
                var type = Type.GetType(arg.BasicProperties.ContentType);
                var message = _serializer.Deserialize(body, type);

                ((dynamic)handler).Handle(message);

            };

            _channel.QueueBind(queue: queueName,
                                  exchange: "direct_logs",
                                  routingKey: handler.RoutingKey);


            _channel.BasicConsume(queue: queueName,
                                 autoAck: true,
                                 consumer: consumer);

            _subscriber.Add(new SubscriberDesriptor(handler, consumer, queueName));


        }

        public void Unsuscribe<TEvent>(ISubscription<TEvent> subscribe)
        {
            throw new NotImplementedException();
        }

        public Task Run()
        {
            throw new NotImplementedException();
        }

        public Task<TResult> Send<TResult>(ICommand command)
        {
            throw new NotImplementedException();
        }

  
    }
}
