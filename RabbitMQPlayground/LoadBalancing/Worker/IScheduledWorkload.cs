using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.LoadBalancing
{
    public interface IScheduledWorkload
    {
        IWorkload Workload { get; }
        IProducerDescriptor ProducerDescriptor { get; }
    }
}
