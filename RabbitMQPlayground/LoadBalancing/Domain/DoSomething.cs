using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQPlayground.LoadBalancing
{
    public class DoSomething : WorkBase<int, DoSomethingResult>
    {
        public override Task<DoSomethingResult> Execute(int arg)
        {
            var str = Enumerable.Range(0, arg)
                                             .Select(index => index.ToString())
                                             .Aggregate((i1, i2) => $"{i1}{i2}");

            return Task.FromResult(new DoSomethingResult()
            {
                Result = str
            });
        }
    }
}
