using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.Routing
{
    public interface ISubscription
    {
        Guid SubscriptionId { get; }
        string Exchange { get; }
        string RoutingKey { get; }
    }


}
