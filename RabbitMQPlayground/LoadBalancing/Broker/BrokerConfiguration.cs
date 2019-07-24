using System;
using System.Collections.Generic;
using System.Text;
using RabbitMQ.Client;

namespace RabbitMQPlayground.LoadBalancing
{
    public class BrokerConfiguration : IBrokerConfiguration
    {
        public BrokerConfiguration(IConnection connection, string workloadQueue, string workerRegistrationQueue)
        {
            Connection = connection;
            WorkloadQueue = workloadQueue;
            WorkerRegistrationQueue = workerRegistrationQueue;
        }

        public IConnection Connection { get; private set; }

        public string WorkloadQueue { get; private set; }

        public string WorkerRegistrationQueue { get; private set; }
    }
}
