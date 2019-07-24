using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.LoadBalancing
{
    public interface IWorkResult
    {
        bool IsError { get; }
    }
}
