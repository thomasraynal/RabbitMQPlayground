using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQPlayground.LoadBalancing
{
    public abstract class WorkBase<TArgument, TResult> : IWork<TArgument, TResult> where TResult : class, IWorkResult
    {
        public abstract Task<TResult> Execute(TArgument arg);

        public async Task<object> Execute(dynamic arg)
        {
            return await this.Execute((TArgument)arg);
        }
    }
}
