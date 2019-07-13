using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RabbitMQPlayground.Routing.Event;

namespace RabbitMQPlayground.Routing.Domain
{
    public class Market : IPublisher
    {
        private readonly IBus _bus;

        public List<CurrencyPair> CurrencyPairs { get; }

        public Market(IEventSerializer eventSerializer)
        {
            _bus = new Bus("localhost", eventSerializer);

            _bus.Subscribe(new CommandSubscription<ChangePriceCommand, ChangePriceCommandResult>("fx-commands", "#", (command) =>
            {
                var ccyPair = CurrencyPairs.FirstOrDefault(ccy => ccy.Id == command.AggregateId);

                if (null == ccyPair)
                {
                    ccyPair = new CurrencyPair(command.AggregateId);
                    CurrencyPairs.Add(ccyPair);
                }

                ccyPair.Ask = command.Ask;
                ccyPair.Bid = command.Bid;

                ccyPair.AppliedEvents.Add(command);

                Emit(new PriceChangedEvent(command.AggregateId)
                {
                    Ask = command.Ask,
                    Bid = command.Bid,
                    Counterparty = command.Counterparty,
                }, "fx-events");

                return new ChangePriceCommandResult();

         
            }));
        }

        public void Emit(IEvent @event, string exchange)
        {
            _bus.Emit(@event, exchange);
        }

        public async Task<TCommandResult> Send<TCommandResult>(ICommand command, TimeSpan timeout) where TCommandResult : ICommandResult
        {
            return await _bus.Send<TCommandResult>(command, timeout);
        }
    }
}
