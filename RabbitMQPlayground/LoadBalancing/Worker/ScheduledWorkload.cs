using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.LoadBalancing
{
    public class ScheduledWorkload<TArgument, TResult> : IScheduledWorkload<TArgument, TResult> where TResult : class, IWorkResult
    {
        public ScheduledWorkload(IWorkload<TArgument, TResult> workload, IProducerDescriptor producerDescriptor)
        {
            Workload = workload;
            ProducerDescriptor = producerDescriptor;
        }

        public IWorkload<TArgument, TResult> Workload { get; private set; }

        public IProducerDescriptor ProducerDescriptor { get; private set; }
    }
}
