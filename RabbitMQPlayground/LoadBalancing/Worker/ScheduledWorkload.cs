using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.LoadBalancing
{
    public class ScheduledWorkload : IScheduledWorkload
    {
        public ScheduledWorkload(IWorkload workload, IProducerDescriptor producerDescriptor)
        {
            Workload = workload;
            ProducerDescriptor = producerDescriptor;
        }

        public IWorkload Workload { get; private set; }

        public IProducerDescriptor ProducerDescriptor { get; private set; }
    }
}
