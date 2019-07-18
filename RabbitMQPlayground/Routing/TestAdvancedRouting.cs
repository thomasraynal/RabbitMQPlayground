using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQPlayground.Routing.Domain;
using RabbitMQPlayground.Routing.Event;
using RabbitMQPlayground.Routing.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQPlayground.Routing
{

    [TestFixture]
    public class TestAdvancedRouting
    {
        private readonly string _fxEventsExchange = "fx";
        private readonly string _fxRejectedEventsExchange = "fx-rejected";
        private readonly string _marketQueue = "fxconnect";

        class TestEvent : EventBase
        {
            public TestEvent(string aggregateId) : base(aggregateId)
            {
            }

            public object Invalid { get; set; }

            public string Broker { get; set; }

            [RoutingPosition(1)]
            public string Market { get; set; }

            [RoutingPosition(2)]
            public string Counterparty { get; set; }

            [RoutingPosition(3)]
            public string Exchange { get; set; }


        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.ExchangeDelete(_fxEventsExchange);
                channel.ExchangeDelete(_fxRejectedEventsExchange);
                channel.ExchangeDelete(Bus.CommandsExchange);
                channel.ExchangeDelete(Bus.RejectedCommandsExchange);
            }
        }

        [Test]
        public void ShouldNotSerializeAnEventAsRabbitSubject()
        {
            //non routable property
            Expression<Func<TestEvent, bool>> validExpression = (ev) => (ev.AggregateId == "MySmallBusiness" && (ev.Broker == "Newedge" && ev.Counterparty == "SGCIB") && ev.Exchange == "SmallCap");

            var visitor = new RabbitMQSubjectExpressionVisitor(typeof(TestEvent));

            Assert.Throws<InvalidOperationException>(() =>
            {
                visitor.Visit(validExpression);
            });


            //type not allowed
            validExpression = (ev) => ev.Invalid ==  null;

            Assert.Throws<InvalidOperationException>(() =>
            {
                visitor.Visit(validExpression);
            });


            //member multiple reference
            validExpression = (ev) => ev.AggregateId == "EUR/USD"  && ev.AggregateId == "EUR/GBP";

            Assert.Throws<InvalidOperationException>(() =>
            {
                visitor.Visit(validExpression);
            });

            //too complex lambda 
            validExpression = (ev) => ev.AggregateId == "EUR/USD" && ev.Market != "Euronext";

            Assert.Throws<InvalidOperationException>(() =>
            {
                visitor.Visit(validExpression);
            });
 
        }

        [Test]
        public void ShouldSerializeAnEventAsRabbitSubject()
        {

            Expression<Func<TestEvent, bool>> validExpression = (ev) => (ev.AggregateId == "MySmallBusiness" &&  (ev.Market == "Euronext" && ev.Counterparty == "SGCIB") && ev.Exchange == "SmallCap");

            var visitor = new RabbitMQSubjectExpressionVisitor(typeof(TestEvent));

            visitor.Visit(validExpression);

            var subject = visitor.Resolve();

            Assert.AreEqual("MySmallBusiness.Euronext.SGCIB.SmallCap", subject);

            validExpression = (ev) => true;

            visitor.Visit(validExpression);

            subject = visitor.Resolve();

            Assert.AreEqual("#", subject);

            validExpression = (ev) => ev.AggregateId == "MySmallBusiness";

            visitor.Visit(validExpression);

            subject = visitor.Resolve();

            Assert.AreEqual("MySmallBusiness.*", subject);

            validExpression = (ev) => ev.Market == "Euronext";

            visitor.Visit(validExpression);

            subject = visitor.Resolve();

            Assert.AreEqual("*.Euronext.*", subject);

        }


        [Test]
        public async Task ShouldSendCommand()
        {
            var serializer = new JsonNetSerializer();
            var eventSerializer = new EventSerializer(serializer);

            var logger = new LoggerForTests();

            var factory = new ConnectionFactory() { HostName = "localhost" };

            var busConfiguration = new BusConfiguration(false);

            using (var traderConnection = factory.CreateConnection())
            using (var marketConnection = factory.CreateConnection())
            {

                var trader = new Trader(_fxEventsExchange, (ev)=> true, busConfiguration, traderConnection, logger, eventSerializer);
                var market = new Market(_marketQueue, _fxEventsExchange, busConfiguration, traderConnection, logger, eventSerializer);

                var command = new ChangePriceCommand("EUR/USD", _marketQueue)
                {
                    Ask = 1.25,
                    Bid = 1.15,
                    Counterparty = "SGCIB"
                };

                var commmandResult = await trader.Send<ChangePriceCommandResult>(command);

                Assert.IsNotNull(commmandResult);
                Assert.AreEqual(_marketQueue, commmandResult.Market);

                await Task.Delay(200);

                Assert.AreEqual(1, trader.CurrencyPairs.Count);

                var ccyPair = trader.CurrencyPairs.First();

                Assert.AreEqual(1, ccyPair.AppliedEvents.Count);

                var appliedEvent = ccyPair.AppliedEvents.First() as PriceChangedEvent;

                Assert.AreEqual(command.AggregateId, ccyPair.Id);
                Assert.AreEqual(command.Ask, ccyPair.Ask);
                Assert.AreEqual(command.Bid, ccyPair.Bid);
                Assert.AreEqual(command.Counterparty, appliedEvent.Counterparty);

            }

        }

        [Test]
        public async Task ShouldFilterByTopic()
        {
            var serializer = new JsonNetSerializer();
            var eventSerializer = new EventSerializer(serializer);
      
            var logger = new LoggerForTests();
            var factory = new ConnectionFactory() { HostName = "localhost" };
            var busConfiguration = new BusConfiguration(false);

            using (var traderConnection = factory.CreateConnection())
            {
                var trader = new Trader(_fxEventsExchange, (ev) => ev.Counterparty == "SGCIB", busConfiguration, traderConnection, logger, eventSerializer);

                var validEvent1 = new PriceChangedEvent("EUR/USD")
                {
                    Ask = 1.25,
                    Bid = 1.15,
                    Counterparty = "SGCIB"
                };

                trader.Emit(validEvent1);

                await Task.Delay(50);

                Assert.AreEqual(1, trader.CurrencyPairs.Count);

                var ccyPair = trader.CurrencyPairs.First();

                Assert.AreEqual(1, ccyPair.AppliedEvents.Count);

                var invalidEvent = new PriceChangedEvent("EUR/USD")
                {
                    Ask = 1.25,
                    Bid = 1.15,
                    Counterparty = "BNP"
                };

                trader.Emit(invalidEvent);

                await Task.Delay(50);

                Assert.AreEqual(1, ccyPair.AppliedEvents.Count);

                var validEvent2 = new PriceChangedEvent("EUR/USD")
                {
                    Ask = 1.25,
                    Bid = 1.15,
                    Counterparty = "SGCIB"
                };

                trader.Emit(validEvent2);

                await Task.Delay(50);

                Assert.AreEqual(2, ccyPair.AppliedEvents.Count);
            }

        }

        [Test]
        public async Task ShouldSubscribeAndUnsubscribe()
        {

            var serializer = new JsonNetSerializer();
            var eventSerializer = new EventSerializer(serializer);
            var logger = new LoggerForTests();
            var factory = new ConnectionFactory() { HostName = "localhost" };
            var busConfiguration = new BusConfiguration(false);

            using (var connection = factory.CreateConnection())
            {

                var bus = new Bus(busConfiguration, connection, logger, eventSerializer);

                var sgcibHasBeenCalled = false;
                var bnpHasBeenCalled = false;

                void reset()
                {
                     sgcibHasBeenCalled = false;
                     bnpHasBeenCalled = false;
                };

                var sgcibSubscription = new EventSubscription<PriceChangedEvent>(_fxEventsExchange, ev => ev.Counterparty == "SGCIB", (@event) =>
                {
                    sgcibHasBeenCalled = true;
                });

                var bnpSubscription = new EventSubscription<PriceChangedEvent>(_fxEventsExchange, ev => ev.Counterparty == "BNP", (@event) =>
                {
                    bnpHasBeenCalled = true;
                });

                var sgcibEvent = new PriceChangedEvent("EUR/USD")
                {
                    Ask = 1.25,
                    Bid = 1.15,
                    Counterparty = "SGCIB"
                };

                var bnpEvent = new PriceChangedEvent("EUR/USD")
                {
                    Ask = 1.25,
                    Bid = 1.15,
                    Counterparty = "BNP"
                };

                bus.Emit(sgcibEvent, _fxEventsExchange);
                await Task.Delay(50);

                Assert.IsFalse(sgcibHasBeenCalled);
                Assert.IsFalse(bnpHasBeenCalled);

                bus.Subscribe(sgcibSubscription);

                bus.Emit(sgcibEvent, _fxEventsExchange);
                await Task.Delay(50);

                Assert.IsTrue(sgcibHasBeenCalled);
                Assert.IsFalse(bnpHasBeenCalled);

                reset();

                bus.Subscribe(bnpSubscription);

                bus.Emit(bnpEvent, _fxEventsExchange);
                await Task.Delay(50);

                Assert.IsFalse(sgcibHasBeenCalled);
                Assert.IsTrue(bnpHasBeenCalled);

                reset();

                bus.Unsubscribe(bnpSubscription);

                bus.Emit(bnpEvent, _fxEventsExchange);
                await Task.Delay(50);

                Assert.IsFalse(sgcibHasBeenCalled);
                Assert.IsFalse(bnpHasBeenCalled);

                bus.Emit(sgcibEvent, _fxEventsExchange);
                await Task.Delay(50);

                Assert.IsTrue(sgcibHasBeenCalled);
                Assert.IsFalse(bnpHasBeenCalled);

                reset();

                bus.Unsubscribe(sgcibSubscription);

                bus.Emit(sgcibEvent, _fxEventsExchange);
                await Task.Delay(50);

                Assert.IsFalse(sgcibHasBeenCalled);
                Assert.IsFalse(bnpHasBeenCalled);

            }

        }

        [Test]
        public async Task ShouldFailedToConsumeEvent()
        {
            var serializer = new JsonNetSerializer();
            var eventSerializer = new EventSerializer(serializer);

            var logger = new LoggerForTests();

            var factory = new ConnectionFactory() { HostName = "localhost" };

            var busConfiguration = new BusConfiguration(false);

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            using (var traderConnection = factory.CreateConnection())
            {
                var trader = new Trader(_fxEventsExchange, (ev) => true, busConfiguration, traderConnection, logger, eventSerializer);

                var deadLettersQueue = channel.QueueDeclare(exclusive: true, autoDelete: true).QueueName;
                var consumer = new EventingBasicConsumer(channel);

                channel.QueueBind(queue: deadLettersQueue,
                     exchange: _fxRejectedEventsExchange,
                     routingKey: "#");

                channel.BasicConsume(queue: deadLettersQueue,
                                     autoAck: false,
                                     consumer: consumer);

                var hasReceivedDeadLetter = false;

                consumer.Received += (model, arg) =>
                {
                    hasReceivedDeadLetter = true;
                };

              var body = Encoding.UTF8.GetBytes("this will explode server side");

                channel.BasicPublish(exchange: _fxEventsExchange,
                                     routingKey: "#",
                                     basicProperties: null,
                                     body: body);

                await Task.Delay(500);

                Assert.IsTrue(hasReceivedDeadLetter);

            }

        }

        [Test]
        public async Task ShouldFailedToHandleCommand()
        {
            var serializer = new JsonNetSerializer();
            var eventSerializer = new EventSerializer(serializer);

            var logger = new LoggerForTests();

            var factory = new ConnectionFactory() { HostName = "localhost" };

            var busConfiguration = new BusConfiguration(false)
            {
                CommandTimeout = TimeSpan.FromMilliseconds(500)
            };

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            using (var marketConnection = factory.CreateConnection())
            using (var traderConnection = factory.CreateConnection())
            {
                var trader = new Trader(_fxEventsExchange, (ev) => true, busConfiguration, traderConnection, logger, eventSerializer);

                var deadLettersQueue = channel.QueueDeclare(exclusive: true, autoDelete: true).QueueName;
                var consumer = new EventingBasicConsumer(channel);

                channel.QueueBind(queue: deadLettersQueue,
                     exchange: Bus.RejectedCommandsExchange,
                     routingKey: "#");

                channel.BasicConsume(queue: deadLettersQueue,
                                     autoAck: false,
                                     consumer: consumer);

                var hasReceivedDeadLetter = false;

                consumer.Received += (model, arg) =>
                {
                    hasReceivedDeadLetter = true;
                };

                Assert.ThrowsAsync<TaskCanceledException>(async () =>
                {
                    await trader.Send<ChangePriceCommandResult>(new ChangePriceCommand("EUR/USD", _marketQueue));
                });

                //create an handler for the command
                var market = new Market(_marketQueue, _fxEventsExchange, busConfiguration, traderConnection, logger, eventSerializer);

                var body = Encoding.UTF8.GetBytes("this will explode server side");

                channel.BasicPublish(exchange: Bus.CommandsExchange,
                                     routingKey: _marketQueue,
                                     basicProperties: null,
                                     body: body);

                await Task.Delay(500);

                Assert.IsTrue(hasReceivedDeadLetter);
            }
        }

        [Test]
        public async Task ShouldConsumeEvent()
        {
            var serializer = new JsonNetSerializer();
            var eventSerializer = new EventSerializer(serializer);

            var logger = new LoggerForTests();

            var factory = new ConnectionFactory() { HostName = "localhost" };

            var busConfiguration = new BusConfiguration(false);

            using (var traderConnection = factory.CreateConnection())
            {

                var trader = new Trader(_fxEventsExchange, (ev) => true, busConfiguration, traderConnection, logger, eventSerializer);

                var emittedEvent = new PriceChangedEvent("EUR/USD")
                {
                    Ask = 1.25,
                    Bid = 1.15,
                    Counterparty = "SGCIB"
                };

                trader.Emit(emittedEvent);

                await Task.Delay(50);

                Assert.AreEqual(1, trader.CurrencyPairs.Count);

                var ccyPair = trader.CurrencyPairs.First();

                Assert.AreEqual(1, ccyPair.AppliedEvents.Count);

                var appliedEvent = ccyPair.AppliedEvents.First() as PriceChangedEvent;

                Assert.AreEqual(emittedEvent.AggregateId, ccyPair.Id);
                Assert.AreEqual(emittedEvent.Ask, ccyPair.Ask);
                Assert.AreEqual(emittedEvent.Bid, ccyPair.Bid);
                Assert.AreEqual(emittedEvent.Counterparty, appliedEvent.Counterparty);

         
            }


        }

    }
}
