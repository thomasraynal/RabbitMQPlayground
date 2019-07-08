using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RabbitMQPlayground.Routing.Domain
{
    public class Trader: IPublisher
    {
        public List<CurrencyPair> CurrencyPairs { get; }

        private readonly IBus _bus;

        public Trader(IBus bus)
        {
            CurrencyPairs = new List<CurrencyPair>();

            _bus = bus;

            _bus.Subscribe(new Subscription<PriceChangedEvent>("FX", "*", Handle));
        }

        public void Handle(PriceChangedEvent @event)
        {
            var ccyPair = CurrencyPairs.FirstOrDefault(ccy => ccy.Id == @event.AggregateId);

            if(null== ccyPair)
            {
                ccyPair = new CurrencyPair(@event.AggregateId);
                CurrencyPairs.Add(ccyPair);
            }

            ccyPair.Ask = @event.Ask;
            ccyPair.Bid = @event.Bid;

            ccyPair.AppliedEvents.Add(@event);
        }

        public void Emit(IEvent @event)
        {
            _bus.Emit(@event);
        }

        public async Task<TResult> Send<TResult>(ICommand command)
        {
            return await _bus.Send<TResult>(command);
        }
    }
}
