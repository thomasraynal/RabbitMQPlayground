using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.LoadBalancing
{
    public class DoAnotherThingResult : IWorkResult
    {
        public bool IsError { get; set; }
        public int Result { get; set; }
        public Guid WorkerId { get; set; }
    }
}
