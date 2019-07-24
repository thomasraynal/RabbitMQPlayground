using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.LoadBalancing
{
    public class WorkerDescriptor : IWorkerDescriptor
    {
        public string Id { get; set; }
    }
}
