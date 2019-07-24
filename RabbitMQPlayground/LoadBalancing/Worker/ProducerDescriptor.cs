using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.LoadBalancing
{
    public class ProducerDescriptor : IProducerDescriptor
    {
        public ProducerDescriptor(string id, string correlationId)
        {
            Id = id;
            CorrelationId = correlationId;
        }

        public string Id { get; private set; }
        public string CorrelationId { get; private set; }
    }
}
