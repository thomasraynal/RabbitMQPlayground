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
    public class TestWorkQueue
    {
 
        [Test]
        public async Task TestingWorkQueue()
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
                                     autoDelete: true,
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
