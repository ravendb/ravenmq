using Newtonsoft.Json.Linq;
using Raven.Munin;

namespace RavenMQ.Storage
{
    public class GeneralStorageActions
    {
        private readonly Table identity;

        public GeneralStorageActions(Table identity)
        {
            this.identity = identity;
        }

        public long GetNextIdentityValue(string name)
        {
            var result = identity.Read(new JObject { { "name", name } });
            if (result == null)
            {
                identity.UpdateKey(new JObject
                {
                    {"name",name},
                    {"id", 1}
                });
                return 1;
            }
            var val = result.Key.Value<int>("id") + 1;
            result.Key["id"] = val;
            identity.UpdateKey(result.Key);
            return val;
        }
    }
}