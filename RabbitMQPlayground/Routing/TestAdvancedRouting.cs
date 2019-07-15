using NUnit.Framework;
using RabbitMQPlayground.Routing.Domain;
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
        [Test]
        public void ExpressionSerialization()
        {



            var ev = new PriceChangedEvent("EUR/USD")
            {
                Ask = 1.25,
                Bid = 1.15,
                Counterparty = "SGCIB"
            };

            var ev2 = new PriceChangedEvent("EUR/USD")
            {
                Ask = 1.25,
                Bid = 1.15,
                Counterparty = "BNP"
            };



            var match1 = Expression.Constant("SGCIB");
            var match2 = Expression.Constant("BNP");
            var arg = Expression.Parameter(typeof(PriceChangedEvent), "s");
            var cpty = Expression.Property(arg, "Counterparty");

            var exp1 = Expression.Equal(cpty, match1);
            var exp2 = Expression.NotEqual(cpty, match2);

            var andAlso1 = Expression.AndAlso(exp1, exp2);

            var andAlso2 = Expression.AndAlso(andAlso1, exp2);

            var lambda = Expression.Lambda<Func<PriceChangedEvent, bool>>(andAlso2, arg);

            var items = new List<Expression>();

            var body = lambda.Body;



            var func = lambda.Compile();

            Assert.IsTrue(func(ev));
            Assert.IsFalse(func(ev2));

        }


        [Test]
        public async Task TestSendCommand()
        {
            var serializer = new JsonNetSerializer();
            var eventSerializer = new EventSerializer(serializer);

            var fxEventExchange = "fx";
            var marketName = "fxconnect";

            var trader = new Trader(fxEventExchange, "#", eventSerializer);
            var market = new Market(marketName, fxEventExchange, eventSerializer);

            var command = new ChangePriceCommand("EUR/USD", marketName)
            {
                Ask = 1.25,
                Bid = 1.15,
                Counterparty = "SGCIB"
            };

            var commmandResult = await trader.Send<ChangePriceCommandResult>(command, TimeSpan.Zero);

            Assert.IsNotNull(commmandResult);
            Assert.AreEqual(marketName, commmandResult.Market);

        }

        [Test]
        public async Task TestConsumeEvent()
        {
            var serializer = new JsonNetSerializer();
            var eventSerializer = new EventSerializer(serializer);

            var fxEventExchange = "fx";

            var trader = new Trader(fxEventExchange, "#", eventSerializer);

            var emittedEvent = new PriceChangedEvent("EUR/USD")
            {
                Ask = 1.25,
                Bid = 1.15,
                Counterparty = "SGCIB"
            };

            trader.Emit(emittedEvent);

            await Task.Delay(200);

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
