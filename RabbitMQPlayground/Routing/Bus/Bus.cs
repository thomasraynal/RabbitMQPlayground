using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing;
using RabbitMQPlayground.Routing.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQPlayground.Routing
{
    public class Bus : IBus
    {
        class CommandSubscriberDescriptor
        {
            private readonly string _exchange;
            private readonly string _routingKey;

            public CommandSubscriberDescriptor(string exchange, string routingKey, EventingBasicConsumer consumer, string queueName)
            {
                _exchange = exchange;
                _routingKey = routingKey;

                Subscriptions = new List<ISubscription>();
                Consumer = consumer;
                QueueName = queueName;
            }

            public string SubscriptionId => $"{_exchange}.{_routingKey}";

            public List<ISubscription> Subscriptions { get; }
            public EventingBasicConsumer Consumer { get; }
            public string QueueName { get; }
        }

        class EventSubscriberDescriptor
        {
            private readonly string _exchange;
            private readonly string _routingKey;

            public EventSubscriberDescriptor(string exchange, string routingKey, EventingBasicConsumer consumer, string queueName)
            {
                _exchange = exchange;
                _routingKey = routingKey;

                Subscriptions = new List<ISubscription>();
                Consumer = consumer;
                QueueName = queueName;
            }

            public string SubscriptionId => $"{_exchange}.{_routingKey}";

            public List<ISubscription> Subscriptions { get; }
            public EventingBasicConsumer Consumer { get; }
            public string QueueName { get; }
        }

        private readonly ConnectionFactory _factory;
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly List<EventSubscriberDescriptor> _eventSubscriberDescriptors;
        private readonly List<CommandSubscriberDescriptor> _commandSubscriberDescriptors;
        private readonly Dictionary<string, TaskCompletionSource<ICommandResult>> _commandResults;
        private readonly IEventSerializer _eventSerializer;

        public Bus(string host, IEventSerializer eventSerializer)
        {
            Id = Guid.NewGuid();

            _eventSerializer = eventSerializer;
            _eventSubscriberDescriptors = new List<EventSubscriberDescriptor>();
            _commandSubscriberDescriptors = new List<CommandSubscriberDescriptor>();

            _factory = new ConnectionFactory() { HostName = host };
            _connection = _factory.CreateConnection();
            _channel = _connection.CreateModel();

        }

        public Guid Id { get; }

        public void Dispose()
        {
            _connection.Dispose();
            _channel.Dispose();
        }

        public void Emit(IEvent @event, string exchange)
        {
            var body = _eventSerializer.Serializer.Serialize(@event);
            var subject = _eventSerializer.GetSubject(@event);

            var properties = _channel.CreateBasicProperties();

            properties.ContentType = @event.GetType().ToString();
           

            _channel.BasicPublish(exchange: exchange,
                                 routingKey: subject,
                                 basicProperties: properties,
                                 body: body);
        }

        private EventSubscriberDescriptor GetOrCreateSubscriberDescriptor(ISubscription subscription)
        {
            var key = $"{subscription.Exchange}.{subscription.RoutingKey}";

            var subscriberDescriptor = _eventSubscriberDescriptors.FirstOrDefault(s => s.SubscriptionId == key);

            if (null == subscriberDescriptor) {

                _channel.ExchangeDeclare(exchange: subscription.Exchange, type: "topic");

                var queueName = _channel.QueueDeclare().QueueName;

                var consumer = new EventingBasicConsumer(_channel);

                _channel.QueueBind(queue: queueName,
                                      exchange: subscription.Exchange,
                                      routingKey: subscription.RoutingKey);

                _channel.BasicConsume(queue: queueName,
                                     autoAck: true,
                                     consumer: consumer);

                subscriberDescriptor = new EventSubscriberDescriptor(subscription.Exchange, subscription.RoutingKey, consumer, queueName);

                consumer.Received += (model, arg) =>
                {
                    var body = arg.Body;
                    var type = Type.GetType(arg.BasicProperties.ContentType);
                    var message = (IEvent)_eventSerializer.Serializer.Deserialize(body, type);

                    foreach (var subscriber in subscriberDescriptor.Subscriptions)
                    {
                        ((dynamic)subscriber).OnEvent(message);
                    }

                };

                _eventSubscriberDescriptors.Add(subscriberDescriptor);

            }

            return subscriberDescriptor;
        }


        public void Subscribe<TEvent>(IEventSubscription<TEvent> subscription)
        {
            var subscriberDescriptor = GetOrCreateSubscriberDescriptor(subscription);
            subscriberDescriptor.Subscriptions.Add(subscription);
        }

        public void Unsubscribe<TEvent>(IEventSubscription<TEvent> subscription)
        {
            var key = $"{subscription.Exchange}.{subscription.RoutingKey}";

            var subscriberDescriptor = _eventSubscriberDescriptors.FirstOrDefault(s => s.SubscriptionId == key);

            if (null == subscriberDescriptor) return;

            subscriberDescriptor.Subscriptions.Remove(subscription);
        }

        public Task Run()
        {
            throw new NotImplementedException();
        }

        private void CreateResultHandler(string queueName, string correlationId)
        {
            var resultHandler = new EventingBasicConsumer(_channel);

            resultHandler.Received += (model, arg) =>
            {
                var body = arg.Body;
                var response = Encoding.UTF8.GetString(body);
                if (arg.BasicProperties.CorrelationId == correlationId)
                {
                    var task = _commandResults[correlationId];
                    var type = Type.GetType(arg.BasicProperties.ContentType);
                    var message = (ICommandResult)_eventSerializer.Serializer.Deserialize(body, type);

                    task.SetResult(message);

                    _channel.QueueDeleteNoWait(queueName);
                }
            };

            _channel.BasicConsume(
               consumer: resultHandler,
               queue: queueName,
               autoAck: true);
        }

        public Task<TCommandResult> Send<TCommandResult>(ICommand command, TimeSpan timeout) where TCommandResult : ICommandResult
        {
            var task = new TaskCompletionSource<ICommandResult>();

            var properties = _channel.CreateBasicProperties();
            var correlationId = Guid.NewGuid().ToString();
            var replyQueueName = _channel.QueueDeclare().QueueName;

            var body = _eventSerializer.Serializer.Serialize(command);
            var subject = _eventSerializer.GetSubject(command);

            properties.ContentType = command.GetType().ToString();
            properties.CorrelationId = correlationId;
            properties.ReplyTo = replyQueueName;

            CreateResultHandler(replyQueueName, correlationId);

            _channel.BasicPublish(
                exchange: "",
                routingKey: subject,
                basicProperties: properties,
                body: body);

            _commandResults.Add(correlationId, task);

            return task.Task.ContinueWith(t => (TCommandResult)t.Result);

        }

        public void Handle<TCommand, TCommandResult>(ICommandSubscription<TCommand, TCommandResult> subscription)
             where TCommand : class, ICommand
             where TCommandResult : ICommandResult
        {
            throw new NotImplementedException();
        }

        public void UnHandle<TCommand, TCommandResult>(ICommandSubscription<TCommand, TCommandResult> subscription)
             where TCommand : class, ICommand
             where TCommandResult : ICommandResult
        {
            throw new NotImplementedException();
        }
    }
}
