using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.LoadBalancing
{
    public interface IWorkerDescriptor
    {
        Guid Id { get; set; }
    }
}
