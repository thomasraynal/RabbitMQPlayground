using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQPlayground.LoadBalancing
{
    interface IWorker<TArgument, TResult> : IActor
    {
        void Schedule(IScheduledWorkload<TArgument, TResult> workload);
        Task<TResult> Handle(IWork<TArgument, TResult> work, TArgument argument);
    }
}
