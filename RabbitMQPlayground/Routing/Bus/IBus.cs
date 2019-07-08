using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.Routing
{
    public interface IBus : IActor, IPublisher
    { 
        void Subscribe<TEvent>(ISubscription<TEvent> subscribe);
        void Unsuscribe<TEvent>(ISubscription<TEvent> subscribe);
    }
}
