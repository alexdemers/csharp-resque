using Newtonsoft.Json.Linq;
using TagMyDoc.Jobs;

namespace Resque.Jobs
{
    public abstract class BaseJob : IJob
    {
        public Job Job { get; set; }
        public string Queue { get; set; }
        public JObject Args { get; set; }

        public abstract void Perform();
    }
}
