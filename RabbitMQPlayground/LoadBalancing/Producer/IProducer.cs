using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQPlayground.LoadBalancing
{
    public interface IProducer : IActor
    {
        Task<TResult> SendWork<TArgument, TResult>(IWorkload<TArgument, TResult> work) where TResult : class, IWorkResult;
    }
}
