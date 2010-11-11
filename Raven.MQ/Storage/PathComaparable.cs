using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RavenMQ.Storage
{
    public class PathComaparable : IComparable<string>, IComparable
    {
        private readonly string value;
        private readonly IEnumerable<string> values;

        public PathComaparable(string value)
        {
            this.value = value;
            values = SplitByPath(value);
        }

        public static IEnumerable<string> SplitByPath(string value)
        {
            var list = new List<string> { value };
            var sb = new StringBuilder(value);
            var lastIndexOfSlash = value.LastIndexOf('/');
            while (lastIndexOfSlash > 0)
            {
                sb.Length = lastIndexOfSlash;
                var item = sb.ToString();
                if (item != value)
                    list.Add(item);
                lastIndexOfSlash = value.LastIndexOf('/', lastIndexOfSlash - 1);
            }
            if (list.Count > 1)
                list.RemoveAt(list.Count - 1); // remove the just "/queues" part of the path
            return list.ToArray();
        }

        public int CompareTo(string other)
        {
            var compareTo = StringComparer.InvariantCultureIgnoreCase.Compare(value, other);

            if (compareTo == 0)
                return 0;

            if (values.Any(v => v.Equals(other, StringComparison.InvariantCultureIgnoreCase)))
            {
                return 0;
            }

            return compareTo;
        }

        public int CompareTo(object obj)
        {
            var s = obj as string;
            if (s != null)
                return CompareTo(s);
            var pathComaparable = (PathComaparable)obj;
            return CompareTo(pathComaparable.value);
        }
    }
}