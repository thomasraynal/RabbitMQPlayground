﻿using System;
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
        }

        public bool IsDurable { get; }
    }
}