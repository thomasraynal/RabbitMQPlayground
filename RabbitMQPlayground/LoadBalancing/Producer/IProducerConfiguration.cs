using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.LoadBalancing
{
    public interface IProducerConfiguration
    {
        IConnection Connection { get; }
        string BrokerWorkloadQueue { get; }
        TimeSpan CommandTimeout { get; }
    }
}
