using System;
using Newtonsoft.Json.Linq;

namespace Resque.Failures
{
    using Newtonsoft.Json;

    public class Redis : Backend
    {
        public Redis(JObject payload, Exception exception, Worker worker, string queue)
        {
            Exception = exception?.InnerException ?? exception ?? new Exception("Unkown error.");
            Worker = worker;
            Queue = queue;
            Payload = payload;

            var data = new JObject
            {
                new JProperty("failed_at", DateTime.Now),
                new JProperty("payload", Payload),
                new JProperty("exception", Exception.GetType().ToString()),
                new JProperty("error", Exception.Message),
                new JProperty("backtrace", new JArray {Exception.ToString()}),
                new JProperty("worker", Worker.ToString()),
                new JProperty("queue", Queue)
            };

            using (var redis = Resque.PooledRedisClientManager.GetClient())
            {
                redis.PushItemToList("resque:failed", data.ToString(Formatting.None));
            }
        }
    }
}
