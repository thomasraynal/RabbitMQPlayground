using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.LoadBalancing
{
    public class DoThrowResult : IWorkResult
    {
        public bool IsError { get; set; }
        public Guid WorkerId { get; set; }

        public int TryOuts { get; set; }

    }
}
