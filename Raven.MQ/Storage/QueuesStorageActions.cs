using Newtonsoft.Json.Linq;
using Raven.Munin;
using RavenMQ.Data;

namespace RavenMQ.Storage
{
    public class QueuesStorageActions
    {
        private readonly Table queues;

        public QueuesStorageActions(Table queues)
        {
            this.queues = queues;
        }

        public QueueStatistics Statistics(string queue)
        {
            var readResult = queues.Read(new JObject { { "Name", queue } });
            if (readResult == null)
                return null;
            return new QueueStatistics
            {
                Name = readResult.Key.Value<string>("Name"),
                NumberOfMessages = readResult.Key.Value<int>("Count")
            };
        }

        public void IncrementMessageCount(string queue)
        {
            var lastIndexOfSlash = queue.LastIndexOf('/');
            if(lastIndexOfSlash != -1)
            {
                var parentQueue = queue.Substring(0, lastIndexOfSlash);
                if(parentQueue.Length > 0)
                {
                    IncrementMessageCount(parentQueue);
                }
            }
            var readResult = queues.Read(new JObject {{"Name", queue}});
            if(readResult == null)
            {
                queues.UpdateKey(new JObject
                {
                    {"Name", queue},
                    {"Count", 1}
                });
            }
            else
            {
                readResult.Key["Count"] = readResult.Key.Value<int>("Count") + 1;
                queues.UpdateKey(readResult.Key);
            }
        }

        public void DecrementMessageCount(string queue)
        {
            var lastIndexOfSlash = queue.LastIndexOf('/');
            if (lastIndexOfSlash != -1)
            {
                var parentQueue = queue.Substring(0, lastIndexOfSlash);
                if (parentQueue.Length > 0)
                {
                    IncrementMessageCount(queue);
                }
            }
            var readResult = queues.Read(new JObject { { "Name", queue } });
            if (readResult == null)
            {
                return;
            }

            var count = readResult.Key.Value<int>("Count");
            if(count == 1)
            {
                queues.Remove(readResult.Key);
                return;
            }
            readResult.Key["Count"] = count - 1;
            queues.UpdateKey(readResult.Key);
           
        }
    }
}