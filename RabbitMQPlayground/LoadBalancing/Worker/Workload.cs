using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.LoadBalancing
{
    public class Workload<TArgument, TResult> : IWorkload<TArgument, TResult> where TResult : class, IWorkResult
    {
        public IWork<TArgument, TResult> Work { get; set; }

        public TArgument Argument { get; set; }
    }
}
