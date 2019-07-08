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
    public class TestRouting
    {
        [Test]
        public async Task TestingRouting()
        {
            var args = new[] { "a", "b", "c" };
            byte[] received_body = null;
            string received_message = null;

            var getMessage = new Func<string[], string>((arg) =>
            {
                return ((arg.Length > 0) ? string.Join(" ", arg) : "Hello World!");
            });

            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.ExchangeDeclare(exchange: "direct_logs",
                                        type: "direct");


                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    received_body = ea.Body;
                    received_message = Encoding.UTF8.GetString(received_body);
                    var routingKey = ea.RoutingKey;
                };

                var queueName = channel.QueueDeclare().QueueName;


                foreach (var sev in args)
                {
                    channel.QueueBind(queue: queueName,
                                      exchange: "direct_logs",
                                      routingKey: sev);
                }

                channel.BasicConsume(queue: queueName,
                                     autoAck: true,
                                     consumer: consumer);


                var severity = (args.Length > 0) ? args[0] : "info";
                var message = getMessage(args);

                var body = Encoding.UTF8.GetBytes(message);
                channel.BasicPublish(exchange: "direct_logs",
                                     routingKey: severity,
                                     basicProperties: null,
                                     body: body);

                await Task.Delay(200);

                Assert.AreEqual(message, received_message);

            }
        }
    }
}
