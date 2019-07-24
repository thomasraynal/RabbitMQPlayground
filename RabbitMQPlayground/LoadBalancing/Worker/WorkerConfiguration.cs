using System;
using System.Collections.Generic;
using System.Text;
using RabbitMQ.Client;

namespace RabbitMQPlayground.LoadBalancing
{
    public class WorkerConfiguration : IWorkerConfiguration
    {
        public WorkerConfiguration(IConnection connection, string brokerWorkerRegistrationQueue)
        {
            Connection = connection;
            BrokerWorkerRegistrationQueue = brokerWorkerRegistrationQueue;
        }

        public IConnection Connection { get; private set; }

        public string BrokerWorkerRegistrationQueue { get; private set; }
    }
}
