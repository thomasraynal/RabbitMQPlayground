using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQPlayground.Routing.Event;

namespace RabbitMQPlayground.Routing.Domain
{
    public class Market : IPublisher, ICommandHandler, IDisposable
    {
        private readonly IBus _bus;

        public List<CurrencyPair> CurrencyPairs { get; }

        public string Name { get; private set; }

        public Market(string name, string fxExchange, IBusConfiguration configuration, IConnection connection, ILogger logger, IEventSerializer eventSerializer)
        {
            _bus = new Bus(configuration, connection, logger, eventSerializer);

            Name = name;

            CurrencyPairs = new List<CurrencyPair>();

            _bus.Handle(new CommandSubscription<ChangePriceCommand, ChangePriceCommandResult>(Name, (command) =>
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
                }, fxExchange);

                return new ChangePriceCommandResult()
                {
                    Market = Name
                };

         
            }));
        }

        public void Emit(IEvent @event, string exchange)
        {
            _bus.Emit(@event, exchange);
        }

        public async Task<TCommandResult> Send<TCommandResult>(ICommand command) where TCommandResult : ICommandResult
        {
            return await _bus.Send<TCommandResult>(command);
        }

        public void Dispose()
        {
            _bus.Dispose();
        }

        public void Handle<TCommand, TCommandResult>(ICommandSubscription<TCommand, TCommandResult> subscription)
                   where TCommand : class, ICommand
             where TCommandResult : ICommandResult
        {
            _bus.Handle(subscription);
        }

        public void UnHandle<TCommand, TCommandResult>(ICommandSubscription<TCommand, TCommandResult> subscription)
               where TCommand : class, ICommand
             where TCommandResult : ICommandResult
        {
            _bus.UnHandle(subscription);
        }
    }
}
