using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.LoadBalancing
{
    public class ScheduledWorkload : IScheduledWorkload
    {
        public ScheduledWorkload(IWorkload workload, IProducerDescriptor producerDescriptor, bool redelivered)
        {
            Workload = workload;
            ProducerDescriptor = producerDescriptor;
            Redelivered = redelivered;
        }

        public IWorkload Workload { get; private set; }

        public IProducerDescriptor ProducerDescriptor { get; private set; }

        public bool Redelivered { get; private set; }
    }
}
