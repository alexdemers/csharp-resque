using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Resque.Jobs;

namespace Resque
{
    public class Worker
    {
        public enum LogType
        {
            None, Normal, Verbose
        }

        public LogType LogLevel = LogType.Verbose;

        private readonly string[] _queues;
        private bool _shutDown;
        private bool _paused;
        private Job _currentJob;
        public string Id { get; set; }

        public Worker(string[] queues)
        {
            _queues = queues;
            Id = string.Format("{0}:{1}:{2}", Dns.GetHostName(), System.Diagnostics.Process.GetCurrentProcess().Id, String.Join(",", _queues));
        }

        public Worker(string queue) :
            this(new[] { queue })
        {
        }

        public void Work(int interval)
        {
            try
            {
                Startup();

                var threads = new List<Thread>();

                while (true)
                {
                    if (_shutDown)
                    {
                        break;
                    }

                    var jobs = new List<Job>();

                    if (!_paused)
                    {
                        Job job;
                        do
                        {
                            job = Reserve();

                            if (job != null)
                            {
                                jobs.Add(job);
                            }
                        } while (job != null);
                    }

                    if (jobs.Count == 0)
                    {
                        if (interval == 0) break;

                        Log("Sleeping for " + interval * 1000);
                        Thread.Sleep(interval * 1000);
                        continue;
                    }

                    foreach (var job in jobs)
                    {
                        var job1 = job;

                        ThreadStart threadStart = delegate
                        {
                            Log("Got " + job1.Queue);
                            WorkingOn(job1);
                            Perform(job1);
                        };
                        var thread = new Thread(threadStart);
                        threads.Add(thread);
                        thread.Start();
                    }

                    foreach (var thread in threads)
                    {
                        thread.Join();
                    }
                    
                    DoneWorking();
                }
            }
            finally
            {
                UnregisterWorker();
            }
        }

        public void Perform(Job job)
        {
            try
            {
                job.Perform();
            }
            catch (Exception e)
            {
                Log(job + " failed: " + e.Message);
                job.Fail(e);
                return;
            }

            job.UpdateStatus(Status.StatusComplete);
            Log("Done " + job);
        }

        public Job Reserve()
        {
            foreach (var queue in Queues())
            {
                Log("Checking " + queue);
                var job = global::Resque.Job.Reserve(queue);

                if (job == null) continue;
                Log("Found job on " + queue);
                return job;
            }
            return null;
        }

        public string[] Queues()
        {
            return _queues.Contains("*") ? FetchQueues() : _queues;
        }

        public string[] FetchQueues()
        {
            return Resque.Queues().ToArray();
        }

        private void Startup()
        {
            RegisterWorker();
        }

        public void PauseProcessing()
        {
            _paused = true;
            Log("Pausing job processing.");
        }

        public void ResumeProcessing()
        {
            _paused = true;
            Log("Resuming job processing.");
        }

        public void Shutdown()
        {
            _shutDown = true;
            Log("Exiting…");
        }

        private void RegisterWorker()
        {
            using (var redis = Resque.PooledRedisClientManager.GetClient())
            {
                redis.AddItemToSet("resque:workers", ToString());
                redis.Set("resque:worker:" + ToString() + ":started", CurrentTimeFormatted());
            }
        }

        public void UnregisterWorker()
        {
            if (_currentJob != null)
            {
                _currentJob.Fail(new DirtyExitException());
            }

            using (var redis = Resque.PooledRedisClientManager.GetClient())
            {
                redis.RemoveItemFromSet("resque:workers", Id);
                redis.Remove("resque:worker:" + Id);
                redis.Remove("resque:worker:" + Id + ":started");
            }

            Stat.Clear("processed:" + Id);
            Stat.Clear("failed:" + Id);
        }

        public void WorkingOn(Job job)
        {
            job.Worker = this;
            _currentJob = job;
            job.UpdateStatus(Status.StatusRunning);

            var data = new JObject
                           {
                               new JProperty("queue", job.Queue),
                               new JProperty("run_at", CurrentTimeFormatted()),
                               new JProperty("payload", job.Payload)
                           };
            using (var redis = Resque.PooledRedisClientManager.GetClient())
            {
                redis.Set("resque:worker:" + job.Worker, data.ToString());
            }
        }

        public void DoneWorking()
        {
            _currentJob = null;
            Stat.Increment("processed");
            Stat.Increment("processed:" + ToString());
            using (var redis = Resque.PooledRedisClientManager.GetClient())
            {
                redis.Remove("resque:worker:" + ToString());
            }
        }

        public override string ToString()
        {
            return Id;
        }

        public void Log(string message)
        {
            switch (LogLevel)
            {
                case LogType.Normal:
                    Console.WriteLine("*** " + message);
                    break;
                case LogType.Verbose:
                    Console.WriteLine(string.Format("[{0}] {1}", new DateTime().ToString(), message));
                    break;
            }
        }

        public JObject Job()
        {
            using (var redis = Resque.PooledRedisClientManager.GetClient())
            {
                return JsonConvert.DeserializeObject<JObject>(redis.GetValue("resque:worker:" + Id));
            }
            
        }

        public int GetStat(string stat)
        {
            return Stat.Get(stat + ":" + ToString());
        }

        private static string CurrentTimeFormatted()
        {
            return DateTime.Now.ToString("ddd MMM dd hh:mm:ss zzzz yyyy");
        }
    }
}
