using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQPlayground.LoadBalancing
{
    interface IWorker<TArgument, TResult> : IActor
    {
        Task Handle(IWork<TArgument, TResult> work, IProducerDescriptor producerDescriptor);
    }
}
