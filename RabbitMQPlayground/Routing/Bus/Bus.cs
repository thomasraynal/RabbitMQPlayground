using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
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
            public CommandSubscriberDescriptor(EventingBasicConsumer consumer, string queueName, ICommandSubscription subscription)
            {
                Consumer = consumer;
                QueueName = queueName;
                Subscription = subscription;
            }

            public string SubscriptionId => $"{QueueName}";
            public ICommandSubscription Subscription { get; }
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

                Subscriptions = new List<IEventSubscription>();
                Consumer = consumer;
                QueueName = queueName;
            }

            public string SubscriptionId => $"{_exchange}.{_routingKey}";

            public List<IEventSubscription> Subscriptions { get; }
            public EventingBasicConsumer Consumer { get; }
            public string QueueName { get; }
        }

        private readonly ConnectionFactory _factory;
        private readonly IModel _channel;
        private readonly List<EventSubscriberDescriptor> _eventSubscriberDescriptors;
        private readonly List<CommandSubscriberDescriptor> _commandSubscriberDescriptors;
        private readonly Dictionary<string, TaskCompletionSource<ICommandResult>> _commandResults;
        private readonly IEventSerializer _eventSerializer;
        private readonly IBusConfiguration _configuration;
        private readonly ILogger _logger;

        public Bus(IBusConfiguration configuration, IConnection connection, ILogger logger, IEventSerializer eventSerializer)
        {
            Id = Guid.NewGuid();

            _logger = logger;
            _eventSerializer = eventSerializer;
            _configuration = configuration;
            _eventSubscriberDescriptors = new List<EventSubscriberDescriptor>();
            _commandSubscriberDescriptors = new List<CommandSubscriberDescriptor>();

            _channel = connection.CreateModel();
            _commandResults = new Dictionary<string, TaskCompletionSource<ICommandResult>>();

        }

        public Guid Id { get; }

        public void Dispose()
        {
            _channel.Dispose();
        }

        public void Emit(IEvent @event, string exchange)
        {
            var body = _eventSerializer.Serializer.Serialize(@event);
            var subject = _eventSerializer.GetSubject(@event);

            var properties = _channel.CreateBasicProperties();

            properties.Type = @event.GetType().ToString();
           

            _channel.BasicPublish(exchange: exchange,
                                 routingKey: subject,
                                 basicProperties: properties,
                                 body: body);
        }

        private EventSubscriberDescriptor GetOrCreateEventSubscriberDescriptor(IEventSubscription subscription)
        {
            var key = $"{subscription.Exchange}.{subscription.RoutingKey}";

            var subscriberDescriptor = _eventSubscriberDescriptors.FirstOrDefault(s => s.SubscriptionId == key);

            if (null == subscriberDescriptor) {

                _channel.ExchangeDeclare(exchange: subscription.Exchange, type: "topic", durable: _configuration.IsDurable);

                //todo: handle durable bus
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
                    var type = Type.GetType(arg.BasicProperties.Type);
                    var message = (IEvent)_eventSerializer.Serializer.Deserialize(body, type);

                    foreach (var subscriber in subscriberDescriptor.Subscriptions)
                    {
                        subscriber.OnEvent(message);
                    }

                };

                _eventSubscriberDescriptors.Add(subscriberDescriptor);

            }

            return subscriberDescriptor;
        }

    
        public void Subscribe<TEvent>(IEventSubscription<TEvent> subscription)
        {
            var subscriberDescriptor = GetOrCreateEventSubscriberDescriptor(subscription);
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
            return Task.CompletedTask;
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
                    var type = Type.GetType(arg.BasicProperties.Type);
                    var message = (ICommandResult)_eventSerializer.Serializer.Deserialize(body, type);

                    task.SetResult(message);

                    _commandResults.Remove(correlationId);

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

            properties.Type = command.GetType().ToString();
            properties.CorrelationId = correlationId;
            properties.ReplyTo = replyQueueName;

            CreateResultHandler(replyQueueName, correlationId);

            _commandResults.Add(correlationId, task);

            _channel.BasicPublish(
                exchange: string.Empty,
                routingKey: command.Target,
                basicProperties: properties,
                mandatory : true,
                body: body);

            return task.Task.ContinueWith(t => (TCommandResult)t.Result);

        }

        public void Handle<TCommand, TCommandResult>(ICommandSubscription<TCommand, TCommandResult> subscription)
             where TCommand : class, ICommand
             where TCommandResult : ICommandResult
        {
            var target = subscription.Target;

            if (_commandSubscriberDescriptors.Any(subscriber => subscriber.SubscriptionId == target)) throw new InvalidOperationException($"Bus already have an handler for {subscription.SubscriptionId}");

            _channel.QueueDeclare(queue: target, durable: false, exclusive: false, autoDelete: false, arguments: null);

            var consumer = new EventingBasicConsumer(_channel);

            _channel.BasicConsume(queue: target,
                                  autoAck: false,
                                  consumer: consumer);

            var subscriberDescriptor = new CommandSubscriberDescriptor(consumer, target, subscription);

            consumer.Received += (model, arg) =>
            {
                try
                {

                    var properties = arg.BasicProperties;
                    var body = arg.Body;
                    var type = Type.GetType(arg.BasicProperties.Type);
                    var message = (ICommand)_eventSerializer.Serializer.Deserialize(body, type);

                    var descriptor = _commandSubscriberDescriptors.FirstOrDefault(subscriber => subscriber.SubscriptionId == message.Target);
                    var commandResult = descriptor.Subscription.OnCommand(message);

                    var replyProperties = _channel.CreateBasicProperties();
                    replyProperties.CorrelationId = properties.CorrelationId;
                    replyProperties.Type = typeof(TCommandResult).ToString();

                    var replyMessage = _eventSerializer.Serializer.Serialize(commandResult);

                    _channel.BasicPublish(exchange: string.Empty, routingKey: properties.ReplyTo, mandatory: true, basicProperties: replyProperties, body: replyMessage);
                    _channel.BasicAck(deliveryTag: arg.DeliveryTag, multiple: false);

                }
                catch (Exception ex)
                {
                    _channel.BasicNack(deliveryTag: arg.DeliveryTag, multiple: false, requeue: false);

                    _logger.LogError($"Error while handling command {arg.BasicProperties.Type}", ex);

                }

            };

            _commandSubscriberDescriptors.Add(subscriberDescriptor);
        }

        public void UnHandle<TCommand, TCommandResult>(ICommandSubscription<TCommand, TCommandResult> subscription)
             where TCommand : class, ICommand
             where TCommandResult : ICommandResult
        {
            var key = subscription.Target;

            var subscriberDescriptor = _commandSubscriberDescriptors.FirstOrDefault(s => s.SubscriptionId == key);

            if (null == subscriberDescriptor) return;

            _commandSubscriberDescriptors.Remove(subscriberDescriptor);
        }
    }
}
