using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQPlayground.LoadBalancing
{

    public class DoThrow : WorkBase<bool, DoThrowResult>
    {
        public static bool ShouldThrow = true;
        public static int TryOuts = 0;

        public override Task<DoThrowResult> Execute(bool shouldStopThrowing)
        {
            TryOuts++;

            if (!ShouldThrow && shouldStopThrowing)
            {
                return Task.FromResult(new DoThrowResult()
                {
                    TryOuts = TryOuts
                });

            }

            ShouldThrow = false;

            throw new Exception("boom");
        }
    }
}
