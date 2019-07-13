using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.Routing
{
    public interface IEventSubscription<TEvent> : ISubscription
    {
        Action<TEvent> OnTypedEvent { get; }
        Action<IEvent> OnEvent { get; }
    }
}
