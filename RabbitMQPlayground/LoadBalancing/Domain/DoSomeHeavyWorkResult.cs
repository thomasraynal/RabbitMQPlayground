using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.LoadBalancing
{
    public class DoSomeHeavyWorkResult : IWorkResult
    {
        public bool IsError { get; set; }

        public Guid WorkerId { get; set; }
    }
}