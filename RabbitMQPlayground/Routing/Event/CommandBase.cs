using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.Routing.Event
{
    public class CommandBase : EventBase, ICommand
    {
        public CommandBase(string aggregateId) : base(aggregateId)
        {
        }
    }
}
