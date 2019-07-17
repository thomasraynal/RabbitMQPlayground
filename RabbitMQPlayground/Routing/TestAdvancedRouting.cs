using NUnit.Framework;
using RabbitMQ.Client;
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

            var fxEventExchange = "fx";
            var marketName = "fxconnect";

            var logger = new LoggerForTests();

            var factory = new ConnectionFactory() { HostName = "localhost" };

            var busConfiguration = new BusConfiguration(false);

            using (var traderConnection = factory.CreateConnection())
            using (var marketConnection = factory.CreateConnection())
            {

                var trader = new Trader(fxEventExchange, (ev)=> true, busConfiguration, traderConnection, logger, eventSerializer);
                var market = new Market(marketName, fxEventExchange, busConfiguration, traderConnection, logger, eventSerializer);

                var command = new ChangePriceCommand("EUR/USD", marketName)
                {
                    Ask = 1.25,
                    Bid = 1.15,
                    Counterparty = "SGCIB"
                };

                var commmandResult = await trader.Send<ChangePriceCommandResult>(command);

                Assert.IsNotNull(commmandResult);
                Assert.AreEqual(marketName, commmandResult.Market);

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
            var fxEventExchange = "fx";

            var logger = new LoggerForTests();
            var factory = new ConnectionFactory() { HostName = "localhost" };
            var busConfiguration = new BusConfiguration(false);

            using (var traderConnection = factory.CreateConnection())
            {
                var trader = new Trader(fxEventExchange, (ev) => ev.Counterparty == "SGCIB", busConfiguration, traderConnection, logger, eventSerializer);

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

        }

        [Test]
        public async Task ShouldFailedToConsumeEvent()
        {
            var serializer = new JsonNetSerializer();
            var eventSerializer = new EventSerializer(serializer);

            var fxEventExchange = "fx";

            var logger = new LoggerForTests();

            var factory = new ConnectionFactory() { HostName = "localhost" };

            var busConfiguration = new BusConfiguration(false);

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            using (var traderConnection = factory.CreateConnection())
            {

                var trader = new Trader(fxEventExchange, (ev) => true, busConfiguration, traderConnection, logger, eventSerializer);
                var body = Encoding.UTF8.GetBytes("this will explode server side");

                channel.BasicPublish(exchange: fxEventExchange,
                                     routingKey: "#",
                                     basicProperties: null,
                                     body: body);

                await Task.Delay(2000);

            }

        }

        [Test]
        public async Task ShouldConsumeEvent()
        {
            var serializer = new JsonNetSerializer();
            var eventSerializer = new EventSerializer(serializer);

            var fxEventExchange = "fx";

            var logger = new LoggerForTests();

            var factory = new ConnectionFactory() { HostName = "localhost" };

            var busConfiguration = new BusConfiguration(false);

            using (var traderConnection = factory.CreateConnection())
            {

                var trader = new Trader(fxEventExchange, (ev) => true, busConfiguration, traderConnection, logger, eventSerializer);

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
