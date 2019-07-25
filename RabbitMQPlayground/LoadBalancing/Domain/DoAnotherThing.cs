using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQPlayground.LoadBalancing
{
    public class DoAnotherThing : WorkBase<long, DoAnotherThingResult>
    {
        public override Task<DoAnotherThingResult> Execute(long arg)
        {
            var sum = Enumerable.Range(0, (int)arg).Sum();

            return Task.FromResult(new DoAnotherThingResult()
            {
                Result = sum
            });
        }
    }
}
