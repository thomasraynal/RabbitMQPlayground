﻿using Newtonsoft.Json;
using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQPlayground.LoadBalancing.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQPlayground.LoadBalancing
{
    [TestFixture]
    public class TestLoadBalancing
    {
        private const string brokerWorkloadQueue = "works";
        private const string brokerWorkerRegistrationQueue = "register";

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            JsonConvert.DefaultSettings = () =>
            {
                var settings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    TypeNameHandling = TypeNameHandling.Objects,

                };

                settings.Converters.Add(new AbstractConverter<IWork<int, DoSomethingResult>, DoSomething>());

                return settings;
            };
        }

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
        public async Task ShouldSendWorkAndGetResult()
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
            using (var broker = new Broker(brokerConfiguration, serializer))
            using (var worker1 = new Worker<int, DoSomethingResult>(worker1Configuration, serializer))
            using (var producer = new Producer(producerConfiguration, serializer))
            {
                await Task.Delay(500);

                var workload = new Workload<int, DoSomethingResult>()
                {
                    Argument = 10,
                    Work = new DoSomething()
                };

                var result = await producer.SendWork(workload);

                Assert.IsNotNull(result);
                Assert.IsFalse(result.IsError);
                Assert.AreEqual("0123456789", result.Result);


            }


        }

    }
}
