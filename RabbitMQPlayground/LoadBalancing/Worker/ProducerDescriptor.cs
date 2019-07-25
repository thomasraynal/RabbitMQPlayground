using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.LoadBalancing
{
    public class ProducerDescriptor : IProducerDescriptor
    {
        public ProducerDescriptor(string id, string correlationId, ulong deliveryTag)
        {
            Id = id;
            CorrelationId = correlationId;
            DeliveryTag = deliveryTag;
        }

        public string Id { get; private set; }
        public string CorrelationId { get; private set; }

        public ulong DeliveryTag { get; private set; }
    }
}
