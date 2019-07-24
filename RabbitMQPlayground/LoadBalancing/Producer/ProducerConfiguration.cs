using System;
using System.Collections.Generic;
using System.Text;
using RabbitMQ.Client;

namespace RabbitMQPlayground.LoadBalancing
{
    public class ProducerConfiguration : IProducerConfiguration
    {
        public ProducerConfiguration(IConnection connection, string brokerWorkloadQueue, TimeSpan commandTimeout)
        {
            Connection = connection;
            BrokerWorkloadQueue = brokerWorkloadQueue;
            CommandTimeout = commandTimeout;
        }

        public IConnection Connection { get; private set; }

        public string BrokerWorkloadQueue { get; private set; }

        public TimeSpan CommandTimeout { get; private set; }
    }
}
