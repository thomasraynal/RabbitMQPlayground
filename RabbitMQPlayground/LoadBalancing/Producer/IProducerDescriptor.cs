using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.LoadBalancing
{
    public interface IProducerDescriptor
    {
        string Id { get; }
        string CorrelationId { get; }
        ulong DeliveryTag { get; }
    }
}
