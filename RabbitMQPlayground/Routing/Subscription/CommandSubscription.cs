using RabbitMQPlayground.Routing.Event;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.Routing
{
    public class CommandSubscription<TCommand, TCommandResult> : SubscriptionBase, ICommandSubscription<TCommand, TCommandResult> where TCommand : class, ICommand
        where TCommandResult : ICommandResult
    {
        public CommandSubscription(string exchange, string routingKey, Func<TCommand, TCommandResult> onCommand) : base(exchange, routingKey)
        {
            OnTypedCommand = onCommand;

            OnCommand = (command) =>
            {
                return OnTypedCommand(command as TCommand);
            };
        }

        public Func<TCommand, TCommandResult> OnTypedCommand { get; }

        public Func<ICommand, ICommandResult> OnCommand { get; }

        public override bool Equals(object obj)
        {
            return obj is CommandSubscription<TCommand, TCommandResult> subscription &&
                   SubscriptionId.Equals(subscription.SubscriptionId);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(SubscriptionId);
        }

    }
}
