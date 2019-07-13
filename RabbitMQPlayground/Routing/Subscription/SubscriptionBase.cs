using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.Routing
{
    public abstract class SubscriptionBase
    {
        public SubscriptionBase(string exchange, string routingKey)
        {
            Exchange = exchange;
            RoutingKey = routingKey;
            SubscriptionId = Guid.NewGuid();
        }

        public string Exchange { get; }

        public string RoutingKey { get; }

        public Guid SubscriptionId { get; }
    }
}
