using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.LoadBalancing
{
    public interface IWorkerDescriptor
    {
        string Id { get; set; }

    }
}
