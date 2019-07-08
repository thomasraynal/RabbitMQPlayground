using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.Routing
{
    public interface IEvent
    {
        string AggregateId { get; }
        string Subject { get; }
        Type EventType { get; }
    }
}
