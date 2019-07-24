using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.LoadBalancing
{
    public interface IScheduledWorkload<TArgument, TResult>
    {
        IWorkload<TArgument, TResult> Workload { get; }
        IProducerDescriptor ProducerDescriptor { get; }
    }
}
