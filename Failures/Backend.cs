using System;

namespace Resque.Failures
{
    public abstract class Backend
    {
        public Exception Exception { get; set; }
        public Worker Worker { get; set; }
        public string Queue { get; set; }
        public object Payload { get; set; }

        protected Backend()
        {
            
        }

        protected Backend(Object payload, Exception exception, Worker worker, String queue)
        {
            Exception = exception;
            Worker = worker;
            Queue = queue;
            Payload = payload;
        }
    }
}
