using System;
using RabbitMQPlayground.Routing.Event;

namespace RabbitMQPlayground.Routing
{
    public interface ICommandSubscription<TCommand, TCommandResult> : ICommandSubscription
        where TCommand : class, ICommand
        where TCommandResult : ICommandResult
    {
        Func<TCommand, TCommandResult> OnTypedCommand { get; }
    }

    public interface ICommandSubscription
    {
        Guid SubscriptionId { get; }
        string Target { get; }
        Func<ICommand, ICommandResult> OnCommand { get; }
    }
}