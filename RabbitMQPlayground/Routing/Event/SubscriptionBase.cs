using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.Routing
{
    public abstract class EventSubscriptionBase : IEventSubscription
    {
        public EventSubscriptionBase(string exchange, string routingKey)
        {
            Exchange = exchange;
            RoutingKey = routingKey;
            SubscriptionId = Guid.NewGuid();
        }

        public string Exchange { get; }

        public string RoutingKey { get; }

        public Guid SubscriptionId { get; }

        public Action<IEvent> OnEvent { get; protected set; }
    }
}
