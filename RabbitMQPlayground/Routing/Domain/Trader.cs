using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQPlayground.Routing.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace RabbitMQPlayground.Routing.Domain
{
    public class Trader
    {
        public List<CurrencyPair> CurrencyPairs { get; }

        private readonly IBus _bus;
        private readonly string _fxExchange;

        public Trader(string fxExchange, Expression<Func<PriceChangedEvent, bool>> routingStrategy, IBusConfiguration configuration, IConnection connection, ILogger logger, IEventSerializer eventSerializer)
        {
            CurrencyPairs = new List<CurrencyPair>();

            _bus = new Bus(configuration, connection, logger, eventSerializer);

            _fxExchange = fxExchange;

            _bus.Subscribe(new EventSubscription<PriceChangedEvent>(fxExchange, routingStrategy, (@event) =>
            {
                var ccyPair = CurrencyPairs.FirstOrDefault(ccy => ccy.Id == @event.AggregateId);

                if (null == ccyPair)
                {
                    ccyPair = new CurrencyPair(@event.AggregateId);
                    CurrencyPairs.Add(ccyPair);
                }

                ccyPair.Ask = @event.Ask;
                ccyPair.Bid = @event.Bid;

                ccyPair.AppliedEvents.Add(@event);

            }));


        }

        public void Emit(IEvent @event)
        {
            _bus.Emit(@event, _fxExchange);
        }

        public async Task<TCommandResult> Send<TCommandResult>(ICommand command, TimeSpan timeout) where TCommandResult : ICommandResult
        {
            return await _bus.Send<TCommandResult>(command, timeout);
        }
    }
}
