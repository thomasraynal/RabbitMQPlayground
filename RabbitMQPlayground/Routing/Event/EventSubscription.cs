using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.Routing
{
    public class EventSubscription<TEvent> : EventSubscriptionBase, IEventSubscription<TEvent> where TEvent : class, IEvent
    {
        public EventSubscription(string exchange, string routingKey, Action<TEvent> onEvent) : base(exchange, routingKey)
        {

            OnTypedEvent = onEvent;

            OnEvent = (ev) =>
            {
                OnTypedEvent(ev as TEvent);
            };
        }

        public Action<TEvent> OnTypedEvent { get; }

        public override bool Equals(object obj)
        {
            return obj is EventSubscription<TEvent> subscription &&
                   SubscriptionId.Equals(subscription.SubscriptionId);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(SubscriptionId);
        }
    }
}
