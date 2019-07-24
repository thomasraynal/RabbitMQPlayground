using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.LoadBalancing
{
    public class DoSomethingResult : IWorkResult
    {
        public bool IsError { get; set; }
        public String Result { get; set; }
    }
}
