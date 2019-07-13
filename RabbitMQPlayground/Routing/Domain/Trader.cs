using RabbitMQPlayground.Routing.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RabbitMQPlayground.Routing.Domain
{
    public class Trader: IPublisher
    {
        public List<CurrencyPair> CurrencyPairs { get; }

        private readonly IBus _bus;

        public Trader(IEventSerializer eventSerializer)
        {
            CurrencyPairs = new List<CurrencyPair>();

            _bus = new Bus("localhost", eventSerializer);

            _bus.Subscribe(new EventSubscription<PriceChangedEvent>("fx-events", "#", (@event) =>
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

        public void Emit(IEvent @event, string exchange)
        {
            throw new NotImplementedException();
        }

        public Task<TCommandResult> Send<TCommandResult>(ICommand command, TimeSpan timeout) where TCommandResult : ICommandResult
        {
            throw new NotImplementedException();
        }
    }
}
