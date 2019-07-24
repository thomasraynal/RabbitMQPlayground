using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.LoadBalancing
{
    public interface IScheduledWorkload<TArgument, TResult> where TResult : class, IWorkResult
    {
        IWorkload<TArgument, TResult> Workload { get; }
        IProducerDescriptor ProducerDescriptor { get; }
    }
}
