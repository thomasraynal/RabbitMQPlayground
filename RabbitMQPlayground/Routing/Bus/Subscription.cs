using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.Routing
{
    public class Subscription<TEvent> : ISubscription<TEvent>
    {
        public Subscription(string exchange, string routingKey, Action<TEvent> onEvent)
        {
            Exchange = exchange;
            RoutingKey = routingKey;
            OnEvent = onEvent;
        }

        public string Exchange { get; }

        public string RoutingKey { get; }

        public Action<TEvent> OnEvent { get; }
    }
}
