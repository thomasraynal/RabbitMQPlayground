using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQPlayground.LoadBalancing.Domain
{
    public class DoSomething : IWork<int, string>
    {
        public Task<string> Execute(int arg)
        {
            return Task.FromResult(Enumerable.Range(0, arg)
                                             .Select(index => index.ToString())
                                             .Aggregate((i1, i2) => $"{i1}{i2}"));
        }
    }
}
