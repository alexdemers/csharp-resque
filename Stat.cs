using System;

namespace Resque
{
    public class Stat 
    {
        public static int Get(String stat)
        {
            using (var redis = Resque.PooledRedisClientManager.GetClient())
            {
                return Int32.Parse(redis.GetValue("resque:stat:" + stat));
            }
        }

        public static void Increment(String stat, int amt)
        {
            using (var redis = Resque.PooledRedisClientManager.GetClient())
            {
                redis.IncrementValueBy("resque:stat:" + stat, amt);
            }
        }

        public static void Increment(String stat)
        {
            using (var redis = Resque.PooledRedisClientManager.GetClient())
            {
                redis.Increment("resque:stat:" + stat, 1);
            }
        }

        public static void Decrement(String stat)
        {
            using (var redis = Resque.PooledRedisClientManager.GetClient())
            {
                redis.Decrement("resque:stat:" + stat, 1);
            }
        }

        public static void Clear(String stat)
        {
            using (var redis = Resque.PooledRedisClientManager.GetClient())
            {
                redis.Remove("resque:stat:" + stat);
            }
        }
    }
}
