using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.LoadBalancing
{
    public interface IBrokerConfiguration
    {
        IConnection Connection { get; }
        string WorkloadQueue { get; }
        string WorkerRegistrationQueue { get; }
    }
}
