using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.LoadBalancing
{
    public interface IScheduledWorkload
    {
        bool Redelivered { get; }
        IWorkload Workload { get; }
        IProducerDescriptor ProducerDescriptor { get; }
    }
}
