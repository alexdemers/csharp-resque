using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ServiceStack.Redis;

namespace Resque
{
    public class NoQueueError : Exception { }

    public class NoClassError : Exception { }

    public class Resque
    {
        private const string RESQUE_QUEUES_KEY = "resque:queues";
        private const string RESQUE_QUEUE_KEY_PREFIX = "resque:queue:";

        public const double Version = 1.0;
        public static Dictionary<string, Type> RegisteredJobs = new Dictionary<string, Type>();
        public static PooledRedisClientManager PooledRedisClientManager;

        public static void SetRedis(string hostname = "localhost", int port = 6379, long database = 0)
        {
            PooledRedisClientManager = new PooledRedisClientManager(new[] {hostname + ":" + port},
                                                                    new[] {hostname + ":" + port}, database);
        }

        public static void Push(string queue, JObject item)
        {
            using (var redis = PooledRedisClientManager.GetClient())
            {
                redis.AddItemToSet(RESQUE_QUEUES_KEY, queue);
                redis.PushItemToList(RESQUE_QUEUE_KEY_PREFIX + queue, item.ToString());
            }
        }

        public static JObject Pop(string queue)
        {
            using (var redis = PooledRedisClientManager.GetClient())
            {
                var data = redis.PopItemFromList(RESQUE_QUEUE_KEY_PREFIX + queue);
                if (data == null) return null;
                return JsonConvert.DeserializeObject<JObject>(data);
            }
        }

        public static long Size(string queue)
        {
            using (var redis = PooledRedisClientManager.GetClient())
            {
                return redis.GetListCount(RESQUE_QUEUE_KEY_PREFIX + queue);
            }
        }

        public static bool Enqueue(string queue, string className, JObject arguments, bool trackStatus = false)
        {
            var argumentsArray = new JArray
                                     {
                                         arguments
                                     };
            var result = Job.Create(queue, className, argumentsArray, trackStatus);

            if (result)
            {
                Event.OnAfterEnqueue(className, arguments, queue, EventArgs.Empty);
            }

            return result;
        }

        public static Job Reserve(string queue)
        {
            return Job.Reserve(queue);
        }

        public static void AddJob(string className, Type type)
        {
            RegisteredJobs.Add(className, type);
        }

        public static HashSet<string> Queues()
        {
            using (var redis = PooledRedisClientManager.GetClient())
            {
                return redis.GetAllItemsFromSet(RESQUE_QUEUES_KEY);
            }
        }
    }
}
