using System;
using System.Collections.Generic;
using System.Text;
using RabbitMQ.Client;

namespace RabbitMQPlayground.Routing
{
    public class BusConfiguration : IBusConfiguration
    {
        public BusConfiguration(bool isDurable)
        {
            IsDurable = isDurable;
            CommandTimeout = TimeSpan.FromSeconds(1);
        }

        public bool IsDurable { get; }
        public TimeSpan CommandTimeout { get; }
    }
}
