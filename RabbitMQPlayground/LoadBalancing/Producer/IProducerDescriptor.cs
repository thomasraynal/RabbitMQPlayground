using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.LoadBalancing
{
    public interface IProducerDescriptor
    {
        Guid Id { get; set; }
    }
}
