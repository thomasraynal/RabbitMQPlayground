using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.Routing
{
    public interface ISubscription
    {
    }

    public interface ISubscription<TEvent> : ISubscription
    {
        string Exchange { get; }
        string RoutingKey { get; }
        Action<TEvent> OnEvent { get; }
    }
}
