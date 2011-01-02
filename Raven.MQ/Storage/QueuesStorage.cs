using System;
using Raven.Munin;

namespace RavenMQ.Storage
{
    public class QueuesStorage : Database
    {
        public QueuesStorage(IPersistentSource persistentSource) : base(persistentSource)
        {
            Messages = Add(new Table(key => key["MsgId"], "Messages")
            {
                {"ByMsgId", key => new ComparableByteArray(key.Value<byte[]>("MsgId"))}
            });

            PendingMessages = Add(new Table(key => key["MsgId"], "Messages")
            {
                {"ByHideDesc", key=> new ReverseComparable(key.Value<DateTime>("Hide"))}
            });

            Queues = Add(new Table(key => key["Name"], "Queues"));


            Identity = Add(new Table(x => x.Value<string>("name"), "Identity"));
            
            Details = Add(new Table("Details"));
        }

    	public class ReverseComparable : IComparable
    	{
    		private readonly DateTime _inner;

    		public ReverseComparable(DateTime inner)
    		{
    			_inner = inner;
    		}

    		public int CompareTo(object obj)
    		{
    			var reverseComparable = obj as ReverseComparable;
				if (reverseComparable != null)
					return -1*_inner.CompareTo(reverseComparable._inner);
    			return -1*_inner.CompareTo(obj);
    		}
    	}

    	public Table PendingMessages { get; set; }

        public Table Queues { get; set; }

        public Table Identity { get; set; }

        public Table Details { get; set; }

        public Table Messages { get; set; }
    }
}