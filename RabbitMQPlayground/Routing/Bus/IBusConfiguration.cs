using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace RabbitMQPlayground.Routing
{
    public interface IBusConfiguration
    {
        bool IsDurable { get; }
    }
}