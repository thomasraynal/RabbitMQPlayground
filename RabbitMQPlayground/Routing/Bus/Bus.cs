using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQPlayground.Routing.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

        public const string CommandsExchange = "commands";
        public const string RejectedCommandsExchange = "commands-rejected";

        private readonly IModel _channel;
        private readonly string _commandsResultQueue;
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

            _commandsResultQueue = _channel.QueueDeclare(exclusive: true, autoDelete: true).QueueName;

            _channel.ExchangeDeclare(CommandsExchange, "direct");
            _channel.ExchangeDeclare(RejectedCommandsExchange, "fanout");

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
            var routingKey = _eventSerializer.GetRoutingKey(@event);

            var properties = _channel.CreateBasicProperties();

            properties.Type = @event.GetType().ToString();
            properties.ContentType = _eventSerializer.Serializer.ContentMIMEType;
            properties.ContentEncoding = _eventSerializer.Serializer.ContentEncoding;

            _channel.BasicPublish(exchange: exchange,
                                 routingKey: routingKey,
                                 basicProperties: properties,
                                 body: body);

        }

   
        private EventSubscriberDescriptor GetOrCreateEventSubscriberDescriptor(IEventSubscription subscription)
        {
            var key = $"{subscription.Exchange}.{subscription.RoutingKey}";

            var subscriberDescriptor = _eventSubscriberDescriptors.FirstOrDefault(s => s.SubscriptionId == key);

            if (null == subscriberDescriptor) {

             
                //ensure event stream exchange is create, as well as it's dead letters counterpart
                _channel.ExchangeDeclare(exchange: subscription.Exchange, type: "topic", durable: _configuration.IsDurable);
                _channel.ExchangeDeclare($"{subscription.Exchange}-rejected", "fanout");

                var queueName = _channel.QueueDeclare(exclusive: true, autoDelete: true, arguments: new Dictionary<string, object>
                    {
                        {"x-dead-letter-exchange", $"{subscription.Exchange}-rejected"},

                    }).QueueName;

                var consumer = new EventingBasicConsumer(_channel);

                _channel.QueueBind(queue: queueName,
                                   exchange: subscription.Exchange,
                                   routingKey: subscription.RoutingKey);

                _channel.BasicConsume(queue: queueName,
                                     autoAck: false,
                                     consumer: consumer);

                consumer.Received += (model, arg) =>
                {
                    try
                    {

                        var body = arg.Body;
                        var type = Type.GetType(arg.BasicProperties.Type);
                        var message = (IEvent)_eventSerializer.Serializer.Deserialize(body, type);

                        //if the message is correct, we ack it. Error during the subscriber handling process are their responsability.
                        _channel.BasicAck(deliveryTag: arg.DeliveryTag, multiple: false);

                        foreach (var subscriber in subscriberDescriptor.Subscriptions)
                        {
                            //we may have a faulty subscriber, but if the message is viable, all subscribers must process it
                            try
                            {
                                subscriber.OnEvent(message);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError($"Error while handling event {arg.BasicProperties.Type} by subscriber {subscriber.SubscriptionId} {subscriber.Exchange} {subscriber.RoutingKey}", ex);
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        _channel.BasicReject(deliveryTag: arg.DeliveryTag, requeue: false);
                        _logger.LogError($"Error while handling event {arg.BasicProperties.Type}", ex);
                    }

                };

                subscriberDescriptor = new EventSubscriberDescriptor(subscription.Exchange, subscription.RoutingKey, consumer, queueName);

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

                }
            };

            _channel.BasicConsume(
               consumer: resultHandler,
               queue: queueName,
               autoAck: true);
        }

        public Task<TCommandResult> Send<TCommandResult>(ICommand command) where TCommandResult : ICommandResult
        {
            var task = new TaskCompletionSource<ICommandResult>();
            var properties = _channel.CreateBasicProperties();
            var correlationId = Guid.NewGuid().ToString();

            var body = _eventSerializer.Serializer.Serialize(command);

            properties.ContentType = _eventSerializer.Serializer.ContentMIMEType;
            properties.ContentEncoding = _eventSerializer.Serializer.ContentEncoding;
            properties.Type = command.GetType().ToString();
            properties.CorrelationId = correlationId;
            properties.ReplyTo = _commandsResultQueue;

            CreateResultHandler(_commandsResultQueue, correlationId);

            _commandResults.Add(correlationId, task);

            var cancel = new CancellationTokenSource(_configuration.CommandTimeout);
            cancel.Token.Register(() => task.TrySetCanceled(), false);

            _channel.BasicPublish(
                exchange: CommandsExchange,
                routingKey: command.Target,
                basicProperties: properties,
                mandatory : true,
                body: body);

            return task.Task.ContinueWith(t => (TCommandResult)t.Result, cancel.Token);

        }

        public void Handle<TCommand, TCommandResult>(ICommandSubscription<TCommand, TCommandResult> subscription)
             where TCommand : class, ICommand
             where TCommandResult : ICommandResult
        {
            var target = subscription.Target;

            if (_commandSubscriberDescriptors.Any(subscriber => subscriber.SubscriptionId == target)) throw new InvalidOperationException($"Bus already have an handler for {subscription.SubscriptionId}");

            _channel.QueueDeclare(queue: target, durable: false, exclusive: true, autoDelete: true, arguments: new Dictionary<string, object>
                    {
                        {"x-dead-letter-exchange", RejectedCommandsExchange},
                    });

            _channel.QueueBind(queue: target, exchange: CommandsExchange, routingKey: target);

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
                    properties.ContentType = _eventSerializer.Serializer.ContentMIMEType;
                    properties.ContentEncoding = _eventSerializer.Serializer.ContentEncoding;

                    var replyMessage = _eventSerializer.Serializer.Serialize(commandResult);

                    _channel.BasicAck(deliveryTag: arg.DeliveryTag, multiple: false);
                    _channel.BasicPublish(exchange: string.Empty, routingKey: properties.ReplyTo, mandatory: true, basicProperties: replyProperties, body: replyMessage);
                   
                }
                catch (Exception ex)
                {
                    _channel.BasicReject(deliveryTag: arg.DeliveryTag, requeue: false);

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
