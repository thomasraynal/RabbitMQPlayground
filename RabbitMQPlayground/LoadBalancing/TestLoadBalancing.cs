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
        private const string brokerWorkerRegistrationQueue = "register";

        [Test]
        public void ShouldSendWorkAndGetResult()
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            var producerConnection = factory.CreateConnection();s
            var worker1Connection = factory.CreateConnection();
            var serializer = new JsonNetSerializer();

            var producerConfiguration = new ProducerConfiguration(producerConnection, brokerWorkloadQueue, TimeSpan.FromSeconds(5));
            var worker1Configuration = new WorkerConfiguration(worker1Connection, brokerWorkerRegistrationQueue);

            using (var worker1 = new Worker<int,string>(worker1Configuration, serializer))
            using (var producer = new Producer(producerConfiguration, serializer))
            {

            }


        }

    }
}
