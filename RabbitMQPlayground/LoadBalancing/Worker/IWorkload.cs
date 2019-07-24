using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.LoadBalancing
{
    public interface IWorkload<TArgument, TResult> where TResult : class, IWorkResult
    {
        IWork<TArgument, TResult> Work { get; }
        TArgument Argument { get; }
    }
}
