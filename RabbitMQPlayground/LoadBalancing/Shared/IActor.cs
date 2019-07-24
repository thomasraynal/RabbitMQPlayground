using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.LoadBalancing
{
    public interface IActor : IDisposable
    {
        Guid Id { get; }
    }
}
