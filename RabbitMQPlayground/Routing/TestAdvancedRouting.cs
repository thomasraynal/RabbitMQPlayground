using NUnit.Framework;
using RabbitMQPlayground.Routing.Domain;
using System;
using System.Collections.Generic;
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
        public async Task TestE2E()
        {
            var serializer = new JsonNetSerializer();

            var bus = new Bus("localhost", serializer);

            var trader1 = new Trader(bus);

            var result = await trader1.Send<ChangePriceCommandResult>(new ChangePriceCommand("EUR/USD")
            {
                Ask = 1.25,
                Bid = 1.15,
                Counterparty = "SGCIB"
            });

        }

    }
}
