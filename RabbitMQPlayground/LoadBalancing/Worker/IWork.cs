using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQPlayground.LoadBalancing
{
    public interface IWork
    {
        Task<object> Execute(dynamic arg);
    }

    public interface IWork<TArgument, TResult> : IWork where TResult : class, IWorkResult
    {
        Task<TResult> Execute(TArgument arg);
    }
}
