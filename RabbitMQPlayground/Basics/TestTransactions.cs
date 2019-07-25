using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Threading;
using RabbitMQ.Client.Exceptions;

namespace RabbitMQPlayground.Basics
{
    [TestFixture]
    public class TestTransactions
    {

        [Test]
        public void TestingTransactionsKo()
        {

            string received_message = null;

            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.TxSelect();

                channel.QueueDeclare(queue: "hello", durable: false, exclusive: false, autoDelete: true, arguments: null);

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
                                     mandatory: true,
                                     basicProperties: null,
                                     body: body);

                channel.BasicPublish(exchange: "void",
                     routingKey: "hello",
                       mandatory: true,
                     basicProperties: null,
                     body: body);


                Assert.Throws<AlreadyClosedException>(() =>
                {
                    channel.TxCommit();
                });

                Assert.IsNull(received_message);
            }

        }

        [Test]
        public async Task TestingTransactionsOk()
        {

            string received_message = null;

            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.TxSelect();

                channel.QueueDeclare(queue: "hello", durable: false, exclusive: false, autoDelete: true, arguments: null);

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
                                     mandatory: true,
                                     basicProperties: null,
                                     body: body);


                channel.TxCommit();

                await Task.Delay(200);

                Assert.AreEqual(message, received_message);

            }

        }
    }
}
