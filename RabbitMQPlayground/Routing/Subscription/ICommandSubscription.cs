using System;
using RabbitMQPlayground.Routing.Event;

namespace RabbitMQPlayground.Routing
{
    public interface ICommandSubscription<TCommand, TCommandResult> : ISubscription
        where TCommand : class, ICommand
        where TCommandResult : ICommandResult
    {
        Func<ICommand, ICommandResult> OnCommand { get; }
        Func<TCommand, TCommandResult> OnTypedCommand { get; }
    }
}