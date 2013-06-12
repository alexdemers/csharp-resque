using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Resque.Jobs
{
    internal class Status
    {
        public const int StatusWaiting = 1;
        public const int StatusRunning = 2;
        public const int StatusFailed = 3;
        public const int StatusComplete = 4;

        private readonly string _id;
        private bool? _isTracking;

        private static readonly int[] CompleteStatuses = new[] { StatusFailed, StatusComplete };

        public Status(string id)
        {
            _id = id;
        }

        public static void Create(string id)
        {
            var unixTimestamp = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;

            var statusPacket = new JObject
                                   {
                                       new JProperty("status", StatusWaiting),
                                       new JProperty("updated", unixTimestamp),
                                       new JProperty("started", unixTimestamp)
                                   };

            using (var redis = Resque.PooledRedisClientManager.GetClient())
            {
                redis.Set("resque:job:" + id + ":status", statusPacket.ToString());
            }
        }

        public bool IsTracking()
        {
            if (_isTracking == false) return false;

            using (var redis = Resque.PooledRedisClientManager.GetClient())
            {
                if (redis.GetValue(ToString()) != null)
                {
                    _isTracking = false;
                    return false;
                }
            }

            _isTracking = true;
            return true;
        }

        public void Update(int status)
        {
            if (!IsTracking()) return;

            var unixTimestamp = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;

            var statusPacket = new JObject
                                   {
                                       new JProperty("status", status),
                                       new JProperty("updated", unixTimestamp)
                                   };

            using (var redis = Resque.PooledRedisClientManager.GetClient())
            {
                redis.Set(ToString(), statusPacket.ToString());

                if (CompleteStatuses.Contains(status))
                {
                    redis.ExpireEntryIn(ToString(), new TimeSpan(1, 0, 0, 0));
                }
            }
        }

        public int? Get()
        {
            if (_isTracking == false) return null;

            try
            {
                using (var redis = Resque.PooledRedisClientManager.GetClient())
                {
                    var statusPacket =
                        JsonConvert.DeserializeObject<JObject>(redis.GetValue(ToString()));

                    if (statusPacket == null) return null;

                    return statusPacket["status"].Value<int>();
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void Stop()
        {
            using (var redis = Resque.PooledRedisClientManager.GetClient())
            {
                redis.Remove(ToString());
            }
        }

        public override string ToString()
        {
            return "resque:job:" + _id + ":status";
        }
    }
}
