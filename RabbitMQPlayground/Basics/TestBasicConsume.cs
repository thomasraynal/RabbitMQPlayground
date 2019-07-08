using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Threading;

namespace RabbitMQPlayground
{
    [TestFixture]
    public class TestBasicConsume
    {
 

        [Test]
        public async Task TestingBasicConsume()
        {

            string received_message = null;

            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "hello", durable: false, exclusive: false, autoDelete: false, arguments: null);

                var consumer = new EventingBasicConsumer(channel);

                consumer.Received += (model, ea) =>
                {
                    var received_body = ea.Body;
                    received_message = Encoding.UTF8.GetString(received_body);
                };

                channel.BasicConsume(queue: "hello", autoAck: true, consumer: consumer);

                string message = "Hello World!";
                var body = Encoding.UTF8.GetBytes(message);

                channel.BasicPublish(exchange: "",
                                     routingKey: "hello",
                                     basicProperties: null,
                                     body: body);


                await Task.Delay(200);

                Assert.AreEqual(message, received_message);



            }
        }

        [Test]
        public async Task TestWorkQueue()
        {
            var args = new[] { "a", "b", "c" };
            byte[] received_body = null;
            string received_message =null;

            var getMessage = new Func<string[], string>((arg) =>
                {
                    return ((arg.Length > 0) ? string.Join(" ", arg) : "Hello World!");
                });

            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "task_queue",
                                     durable: true,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    received_body = ea.Body;
                    received_message = Encoding.UTF8.GetString(received_body);


                    int dots = received_message.Split('.').Length - 1;
                    Thread.Sleep(dots * 1000);



                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                };

                channel.BasicConsume(queue: "task_queue",
                                     autoAck: false,
                                     consumer: consumer);


                var message = getMessage(args);

                var body = Encoding.UTF8.GetBytes(message);

                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;

                channel.BasicPublish(exchange: "",
                                     routingKey: "task_queue",
                                     basicProperties: properties,
                                     body: body);

                await Task.Delay(200);

                Assert.AreEqual(message, received_message);

            }


        }

    }
}
