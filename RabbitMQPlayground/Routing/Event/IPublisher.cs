using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQPlayground.Routing
{
    public interface IPublisher
    {
        void Emit(IEvent @event);
        Task<TResult> Send<TResult>(ICommand command);
    }
}
