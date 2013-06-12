using System;
using Newtonsoft.Json.Linq;

namespace Resque
{
    public class Event
    {
        public static event BeforePerformHandler BeforePerform = delegate { };
        public static event AfterPerformHandler AfterPerform = delegate { };
        public static event FailureHandler Failure = delegate { };
        public static event AfterEnqueueHandler AfterEnqueue = delegate { };

        public delegate void BeforePerformHandler(Job job, EventArgs eventArgs);
        public delegate void AfterPerformHandler(Job job, EventArgs eventArgs);
        public delegate void FailureHandler(Exception exception, Job job, EventArgs eventArgs);
        public delegate void AfterEnqueueHandler(string className, JObject arguments, string queue, EventArgs eventArgs);

        public static void OnBeforePerform(Job job, EventArgs eventargs)
        {
            var handler = BeforePerform;
            if (handler != null) handler(job, eventargs);
        }

        public static void OnAfterPerform(Job job, EventArgs eventargs)
        {
            var handler = AfterPerform;
            if (handler != null) handler(job, eventargs);
        }

        public static void OnFailure(Exception exception, Job job, EventArgs eventargs)
        {
            var handler = Failure;
            if (handler != null) handler(exception, job, eventargs);
        }

        public static void OnAfterEnqueue(string className, JObject arguments, string queue, EventArgs eventargs)
        {
            var handler = AfterEnqueue;
            if (handler != null) handler(className, arguments, queue, eventargs);
        }
    }
}
