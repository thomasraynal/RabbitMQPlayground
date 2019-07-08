using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Threading;
using System.Collections.Concurrent;

namespace RabbitMQPlayground
{
    [TestFixture]
    public class TestRPC
    {
        private static int Fib(int n)
        {
            if (n == 0 || n == 1) return n;
            return Fib(n - 1) + Fib(n - 2);
        }

        [Test]
        public async Task TestingRPC()
        {

            BlockingCollection<string> respQueue = new BlockingCollection<string>();

            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "rpc_queue", durable: false, exclusive: false, autoDelete: false, arguments: null);
                channel.BasicQos(0, 1, false);

                var server = new EventingBasicConsumer(channel);
                channel.BasicConsume(queue: "rpc_queue", autoAck: false, consumer: server);

                server.Received += (model, ea) =>
                {
                    string response = null;

                    var body = ea.Body;
                    var serverProperties = ea.BasicProperties;
                    var replyProps = channel.CreateBasicProperties();
                    replyProps.CorrelationId = serverProperties.CorrelationId;


                    var message = Encoding.UTF8.GetString(body);
                    int n = int.Parse(message);
                    response = Fib(n).ToString();

                    var responseBytes = Encoding.UTF8.GetBytes(response);
                    channel.BasicPublish(exchange: "", routingKey: serverProperties.ReplyTo, basicProperties: replyProps, body: responseBytes);
                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);

                };



                var replyQueueName = channel.QueueDeclare().QueueName;
                var client = new EventingBasicConsumer(channel);

                var clientProperties = channel.CreateBasicProperties();
                var correlationId = Guid.NewGuid().ToString();
                clientProperties.CorrelationId = correlationId;
                clientProperties.ReplyTo = replyQueueName;

                client.Received += (model, ea) =>
                {
                    var body = ea.Body;
                    var response = Encoding.UTF8.GetString(body);
                    if (ea.BasicProperties.CorrelationId == correlationId)
                    {
                        respQueue.Add(response);
                    }
                };

                var messageBytes = Encoding.UTF8.GetBytes("30");

                channel.BasicPublish(
                    exchange: "",
                    routingKey: "rpc_queue",
                    basicProperties: clientProperties,
                    body: messageBytes);

                channel.BasicConsume(
                    consumer: client,
                    queue: replyQueueName,
                    autoAck: true);

                await Task.Delay(200);

                var resp = respQueue.Take();


                Assert.AreEqual("832040", resp);

            }

        }

    }
}
