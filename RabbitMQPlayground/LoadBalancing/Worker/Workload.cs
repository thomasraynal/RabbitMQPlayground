using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.LoadBalancing
{
    public class Workload : IWorkload
    {
        public IWork Work { get; set; }

        public object Argument { get; set; }
    }
}
