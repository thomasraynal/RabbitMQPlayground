using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.LoadBalancing
{
    public class Broker : IBroker
    {
        public void Handle<TArgument, TResult>(IWorkload<TArgument, TResult> workload)
        {
            throw new NotImplementedException();
        }

        public void Register(IWorkerDescriptor worker)
        {
            throw new NotImplementedException();
        }
    }
}
