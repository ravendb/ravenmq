using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Linq;

namespace RavenMQ.Extensions
{
	/// <summary>
	/// Json extensions 
	/// </summary>
	public static class JsonExtensions
	{
		/// <summary>
		/// Convert a byte array to a JObject
		/// </summary>
		public static JObject ToJObject(this byte [] self)
		{
			return JObject.Load(new BsonReader(new MemoryStream(self))
			{
				DateTimeKindHandling = DateTimeKind.Utc,
			});
		}

        /// <summary>
        /// Convert a byte array to a JObject
        /// </summary>
        public static JObject ToJObject(this Stream self)
        {
            return JObject.Load(new BsonReader(self)
            {
                DateTimeKindHandling = DateTimeKind.Utc,
            });
        }

		/// <summary>
		/// Convert a JToken to a byte array
		/// </summary>
		public static byte[] ToBytes(this JToken self)
		{
			using (var memoryStream = new MemoryStream())
			{
				self.WriteTo(new BsonWriter(memoryStream)
				{
					DateTimeKindHandling = DateTimeKind.Unspecified
				});
				return memoryStream.ToArray();
			}
		}

        /// <summary>
        /// Convert a JToken to a byte array
        /// </summary>
        public static byte[] ToBytesWithLengthPrefix(this JToken self)
        {
            using (var memoryStream = new MemoryStream())
            {
                for (int i = 0; i < 4; i++)//prefixing with len
                {
                    memoryStream.WriteByte(0);
                }
                self.WriteTo(new BsonWriter(memoryStream)
                {
                    DateTimeKindHandling = DateTimeKind.Unspecified
                });
                var bytesWithLengthPrefix = memoryStream.ToArray();
                var lenAsBytes = BitConverter.GetBytes(bytesWithLengthPrefix.Length - 4);
                Array.Copy(lenAsBytes, 0, bytesWithLengthPrefix, 0, 4);
                return bytesWithLengthPrefix;
            }
        }

        /// <summary>
        /// Convert a JToken to a byte array
        /// </summary>
        public static void WriteTo(this JToken self, Stream stream)
        {
            self.WriteTo(new BsonWriter(stream)
            {
                DateTimeKindHandling = DateTimeKind.Unspecified
            });
        }


	    /// <summary>
		/// Deserialize a <param name="self"/> to an instance of <typeparam name="T"/>
		/// </summary>
		public static T JsonDeserialization<T>(this byte [] self)
		{
			return (T)new JsonSerializer().Deserialize(new BsonReader(new MemoryStream(self)), typeof(T));
		}

		/// <summary>
		/// Deserialize a <param name="self"/> to an instance of<typeparam name="T"/>
		/// </summary>
		public static T JsonDeserialization<T>(this JObject self)
		{
			return (T)new JsonSerializer().Deserialize(new JTokenReader(self), typeof(T));
		}
	}
}
