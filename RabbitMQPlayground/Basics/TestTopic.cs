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
    public class TestTopic
    {
        [Test]
        public async Task TestingTopic()
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {

                var args = new[] { "a", "b", "c" };
                byte[] received_body = null;
                string received_message = null;

                var getMessage = new Func<string[], string>((arg) =>
                {
                    return ((arg.Length > 0) ? string.Join(" ", arg) : "Hello World!");
                });


                channel.ExchangeDeclare(exchange: "topic_logs", type: "topic", durable: false, autoDelete: true);

                var queueName = channel.QueueDeclare().QueueName;

                foreach (var bindingKey in args)
                {
                    channel.QueueBind(queue: queueName,
                                      exchange: "topic_logs",
                                      routingKey: bindingKey);
                }

          
                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    received_body = ea.Body;
                    received_message = Encoding.UTF8.GetString(received_body);
                };

                channel.BasicConsume(queue: queueName,
                                     autoAck: true,
                                     consumer: consumer);

                var routingKey = (args.Length > 0) ? args[0] : "anonymous.info";

                var message = getMessage(args);

                var body = Encoding.UTF8.GetBytes(message);

                channel.BasicPublish(exchange: "topic_logs",
                                     routingKey: routingKey,
                                     basicProperties: null,
                                     body: body);

                await Task.Delay(200);

                Assert.AreEqual(message, received_message);


            }
        }

    }
}
