using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.LoadBalancing.Domain
{
    public class DoSomethingResult : IWorkResult
    {
        public bool IsError { get; }
        public String Result { get; }
    }
}
