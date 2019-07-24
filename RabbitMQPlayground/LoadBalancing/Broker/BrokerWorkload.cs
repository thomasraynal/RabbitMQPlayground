using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.LoadBalancing
{
    public class BrokerWorkload
    {
        public BrokerWorkload()
        {
        }

        public BrokerWorkload(Payload payload, string producerId, string correlationId)
        {
            Payload = payload;
            ProducerId = producerId;
            CorrelationId = correlationId;
        }

        public Payload Payload { get; set; }
        public string ProducerId { get; set; }
        public string CorrelationId { get; set; }
    }
}
