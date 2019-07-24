using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace RabbitMQPlayground.LoadBalancing
{
    [Serializable]
    public class WorkHandlingFailureException : Exception
    {
        public IWorkErrorResult Error { get; private set; }

        public WorkHandlingFailureException()
        {
        }

        public WorkHandlingFailureException(IWorkErrorResult error)
        {
            Error = error;
        }

        public WorkHandlingFailureException(string message) : base(message)
        {
        }

        public WorkHandlingFailureException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected WorkHandlingFailureException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
