using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.LoadBalancing
{
    public class WorkErrorResult : IWorkErrorResult
    {
        public bool IsError => true;

        public string ErrorMessage { get; internal set; }
    }
}
