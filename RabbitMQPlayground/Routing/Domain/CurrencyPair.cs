using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.Routing
{
    public class CurrencyPair
    {
        public CurrencyPair(string id)
        {
            Id = id;
            AppliedEvents = new List<IEvent>();
        }

        public List<IEvent> AppliedEvents { get; }

        public string Id { get; }
        public double Ask { get; set; }
        public double Bid { get; set; }
    }
}
