using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQPlayground.LoadBalancing
{
    public class DoSomeHeavyWork : WorkBase<int, DoSomeHeavyWorkResult>
    {
        public async override Task<DoSomeHeavyWorkResult> Execute(int arg)
        {
            await Task.Delay(arg);
            return new DoSomeHeavyWorkResult();
        }
    }
}