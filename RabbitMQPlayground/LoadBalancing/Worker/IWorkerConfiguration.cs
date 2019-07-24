using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.LoadBalancing
{
    public interface IWorkerConfiguration
    {
        IConnection Connection { get; }
        string BrokerWorkerRegistrationQueue { get; }
    }
}
