using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;

namespace RabbitMQPlayground.Routing
{
    public interface IBusConfiguration
    {
        bool IsDurable { get; set; }
        TimeSpan CommandTimeout { get; set; }
    }
}