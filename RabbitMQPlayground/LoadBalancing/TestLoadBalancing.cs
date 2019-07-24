using NUnit.Framework;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.LoadBalancing
{
    [TestFixture]
    public class TestLoadBalancing
    {
        private const string brokerWorkloadQueue = "works";

        [Test]
        public void ShouldSendWorkAndGetResult()
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            var producerConnection = factory.CreateConnection();
            var serializer = new JsonNetSerializer();

            var producerConfiguration = new ProducerConfiguration(producerConnection, brokerWorkloadQueue, TimeSpan.FromSeconds(5));

            using (var producer = new Producer(producerConfiguration, serializer))
            {

            }


        }

    }
}
