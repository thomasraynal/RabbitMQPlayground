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

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDelete(brokerWorkloadQueue);
                channel.QueueDelete(brokerWorkerRegistrationQueue);
            }
        }

        [Test]
        public void ShouldSendWorkAndGetResult()
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };

            var producerConnection = factory.CreateConnection();
            var worker1Connection = factory.CreateConnection();
            var brokerConnection = factory.CreateConnection();

            var serializer = new JsonNetSerializer();

            var producerConfiguration = new ProducerConfiguration(producerConnection, brokerWorkloadQueue, TimeSpan.FromSeconds(5));
            var worker1Configuration = new WorkerConfiguration(worker1Connection, brokerWorkerRegistrationQueue);
            var brokerConfiguration = new BrokerConfiguration(brokerConnection, brokerWorkloadQueue, brokerWorkerRegistrationQueue);

            //todo: IWorker<TWork>
            using (var worker1 = new Worker<int,string>(worker1Configuration, serializer))
            using (var producer = new Producer(producerConfiguration, serializer))
            using (var broker = new Broker(brokerConfiguration, serializer))
            {



            }


        }

    }
}
