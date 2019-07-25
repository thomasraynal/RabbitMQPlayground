using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.LoadBalancing
{
    public interface IWorkload
    {
        IWork Work { get; }
        object Argument { get; }
    }
}
