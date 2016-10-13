using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Resque.Jobs;

namespace Resque
{
    public class Job
    {
        public JObject Payload { get; set; }
        public string Queue { get; set; }
        public Worker Worker { get; set; }

        public int? Status
        {
            get
            {
                return new Status(Payload["id"].ToString()).Get();
            }
        }

        private object _instance;

        public static bool Create(string queue, string className, JArray args, bool monitor = false)
        {
            if (String.IsNullOrEmpty(className))
            {
                throw new NoClassError();
            }

            var id = BitConverter.ToString(MD5.Create().ComputeHash(new Guid().ToByteArray())).Replace("-", "").ToLower();

            if (monitor)
            {
                Jobs.Status.Create(id);
            }

            var data = new JObject
                           {
                               new JProperty("class", className),
                               new JProperty("args", args)
                           };
            Resque.Push(queue, data);
            return true;
        }

        public static Job Reserve(string queue)
        {
            var payload = Resque.Pop(queue);
            return payload == null ? null : new Job(queue, payload);
        }

        public Job(string queue, JObject payload)
        {
            Queue = queue;
            Payload = payload;
        }

        public void UpdateStatus(int status)
        {
            var payloadId = Payload["id"];
            if (payloadId != null)
            {
                var statusInstance = new Status(payloadId.ToString());
                statusInstance.Update(status);
            }  
        } 

        public JObject GetArgs()
        {
            return new JObject { {"Values", Payload["args"]} };
        }

        public object GetInstance()
        {
            if (_instance != null) return _instance;

            var type = Resque.RegisteredJobs[Payload["class"].Value<string>()];

            if (type == null)
            {
                throw new ResqueException("Could not find job class " + Payload["class"] + ".");
            }
            
            _instance = Activator.CreateInstance(type);

            var performMethod = type.GetMethod("Perform", BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);

            if (performMethod == null)
            {
                throw new ResqueException("Job class " + Payload["class"] + " does not contain a 'Perform' method.");
            }

            type.InvokeMember("Job", BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.Public, null, _instance, new object[] { this });
            type.InvokeMember("Args", BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.Public, null, _instance, new object[] { GetArgs() });
            type.InvokeMember("Queue", BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.Public, null, _instance, new object[] { Queue });

            return _instance;
        }

        public bool Perform()
        {
            var instance = GetInstance();

            var type = Resque.RegisteredJobs[Payload["class"].ToString()];

            var performMethod = type.GetMethod("Perform", BindingFlags.Instance | BindingFlags.Public);
            var setUpMethod = type.GetMethod("SetUp", BindingFlags.Instance | BindingFlags.Public);
            var tearDownMethod = type.GetMethod("TearDown", BindingFlags.Instance | BindingFlags.Public);

            try
            {
                Event.OnBeforePerform(this, EventArgs.Empty);

                if (setUpMethod != null)
                {
                    setUpMethod.Invoke(instance, new object[] { });
                }

                performMethod.Invoke(instance, new object[] { });

                if (tearDownMethod != null)
                {
                    tearDownMethod.Invoke(instance, new object[] { });
                }

                Event.OnAfterPerform(this, EventArgs.Empty);
            }
            catch (DontPerformException)
            {
                return false;
            }

            return true;
        }

        public void Recreate()
        {
            var status = new Status(Payload["id"].ToString());
            var monitor = false;

            if (status.IsTracking())
            {
                monitor = true;
            }

            Create(Queue, Payload["class"].Value<string>(), Payload["args"].Value<JArray>(), monitor);
        }

        public void Fail(Exception e)
        {
            Event.OnFailure(e, this, EventArgs.Empty);
            UpdateStatus(Jobs.Status.StatusFailed);
            new Failures.Redis(Payload, e, Worker, Queue);
            Stat.Increment("failed");
            Stat.Increment("failed:" + Worker);
        }

        public override string ToString()
        {
            var name = new List<string> {"Job{" + Queue + "}"};

            if (Payload["id"] != null)
            {
                name.Add("ID: " + Payload["id"]);
            }

            name.Add(Payload["class"].ToString());

            if (Payload["args"] != null)
            {
                name.Add(JsonConvert.SerializeObject(Payload["args"]));
            }
            return "(" + String.Join("|", name) + ")";
        }
    }
}
