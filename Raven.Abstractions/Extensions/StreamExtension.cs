using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Raven.Abstractions.Extensions
{
	/// <summary>
	/// Extensions for working with streams
	/// </summary>
	public static class StreamExtension
	{
        /// <summary>
        /// 	Reads the entire request buffer to memory and
        /// 	return it as a byte array.
        /// </summary>
        public static byte[] ReadData(this Stream steram)
        {
            var list = new List<byte[]>();
            const int defaultBufferSize = 1024 * 16;
            var buffer = new byte[defaultBufferSize];
            var offset = 0;
            int read;
            while ((read = steram.Read(buffer, offset, buffer.Length - offset)) != 0)
            {
                offset += read;
                if (offset == buffer.Length)
                {
                    list.Add(buffer);
                    buffer = new byte[defaultBufferSize];
                    offset = 0;
                }
            }
            var totalSize = list.Sum(x => x.Length) + offset;
            var result = new byte[totalSize];
            var resultOffset = 0;
            foreach (var partial in list)
            {
                Buffer.BlockCopy(partial, 0, result, resultOffset, partial.Length);
                resultOffset += partial.Length;
            }
            Buffer.BlockCopy(buffer, 0, result, resultOffset, offset);
            return result;
        }
        public static Task<byte[]> ReadBuffer(this Stream stream)
        {
            var completionSource = new TaskCompletionSource<byte[]>();
            const int defaultBufferSize = 1024 * 16;
            var buffer = new byte[defaultBufferSize];
            var list = new List<byte>();
            AsyncCallback callback = null;
            callback = ar =>
            {
                int read;
                try
                {
                    read = stream.EndRead(ar);
                }
                catch (Exception e)
                {
                    completionSource.SetException(e);
                    return;
                }
                list.AddRange(buffer.Take(read));
                if (read == 0)
                {
                    completionSource.SetResult(list.ToArray());
                    return;
                }
                stream.BeginRead(buffer, 0, buffer.Length, callback, null);
            };
            stream.BeginRead(buffer, 0, buffer.Length, callback, null);

            return completionSource.Task;
        }


        public static Task<JObject> ReadJObject(this Stream stream)
        {
            return stream
                .ReadBuffer()
                .ContinueWith(readLenTask => readLenTask.Result.ToJObject());
        }

        public static Task Write(this Stream stream, JToken value)
        {
            return stream.Write(value.ToBytes());
        }

        public static Task Write(this Stream stream, byte[] buffer)
        {
            return Task.Factory.FromAsync(stream.BeginWrite, stream.EndWrite, buffer, 0, buffer.Length, null);
        }
	}
}
