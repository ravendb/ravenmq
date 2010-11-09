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
            foreach (var parentQueue in PathComaparable.SplitByPath(queue))
            {
                IncrementMessageCountInternal(parentQueue);
            }
        }

        private void IncrementMessageCountInternal(string queue)
        {
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
            foreach (var parentQueue in PathComaparable.SplitByPath(queue))
            {
                DecrementMessageCountInternal(parentQueue);
            }
        }

        private void DecrementMessageCountInternal(string queue)
        {
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