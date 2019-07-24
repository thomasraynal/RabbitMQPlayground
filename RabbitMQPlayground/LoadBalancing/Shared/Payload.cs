using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.LoadBalancing
{
    public class Payload
    {
        public Type WorkLoadType { get; set; }
        public byte [] WorkLoad { get; set; }
    }
}
