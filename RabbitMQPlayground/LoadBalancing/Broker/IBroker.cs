using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.LoadBalancing
{
    public interface IBroker
    {
        void Handle<TArgument, TResult>(IWorkload<TArgument, TResult> workload);
        void Register(IWorkerDescriptor worker);

    }
}
