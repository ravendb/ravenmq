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
            var list = new List<string>{ value };
            var sb = new StringBuilder(value);
            var lastIndexOfSlash = value.LastIndexOf('/');
            if (lastIndexOfSlash != -1)
            {
                sb.Length = lastIndexOfSlash;
                if (sb.Length > 0)
                {
                    list.Add(sb.ToString());
                }
            }
            return list.ToArray();
        }

        public int CompareTo(string other)
        {
            var compareTo = StringComparer.InvariantCultureIgnoreCase.Compare(values.Last(), other);

            if (compareTo == 0)
                return 0;

            if (values.Any(value => value.Equals(other, StringComparison.InvariantCultureIgnoreCase)))
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
            var pathComaparable = (PathComaparable) obj;
            return CompareTo(pathComaparable.value);
        }
    }
}