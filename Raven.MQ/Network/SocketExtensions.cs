using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RavenMQ.Extensions;

namespace RavenMQ.Network
{
    public static class SocketExtensions
    {
        public static Task<JObject> ReadJObjectFromBuffer(this Socket socket)
        {
            var tcs = new TaskCompletionSource<JObject>();
            socket.ReadBuffer(4)
                .ContinueWith(task =>
                {
                    try
                    {
                        var len = BitConverter.ToInt32(task.Result.Array, task.Result.Offset);
                        socket.ReadBuffer(len)
                            .ContinueWith(readLenTask =>
                            {
                                try
                                {
                                    var ms = new MemoryStream(readLenTask.Result.Array, readLenTask.Result.Offset,
                                                              readLenTask.Result.Count);

                                    tcs.SetResult(ms.ToJObject());
                                }
                                catch (Exception e)
                                {
                                    tcs.SetException(e);
                                }
                            });
                    }
                    catch (Exception e)
                    {
                        tcs.SetException(e);
                    }
                });
            return tcs.Task;
        }

        public static Task<ArraySegment<byte>> ReadBuffer(this Socket socket, int bufferSize)
        {
            var completionSource = new TaskCompletionSource<ArraySegment<byte>>();
            var buffer = new byte[bufferSize];
            var start = 0;
            AsyncCallback callback = null;
            callback = ar =>
            {
                int read;
                try
                {
                    read = socket.EndReceive(ar);
                    start += read;
                }
                catch (Exception e)
                {
                    completionSource.SetException(e);
                    return;
                }
                if (read == 0 || start == bufferSize)
                {
                    completionSource.SetResult(new ArraySegment<byte>(buffer, 0, start));
                    return;
                }
                socket.BeginReceive(buffer, start, bufferSize - start, SocketFlags.None, callback, null);
            };
            socket.BeginReceive(buffer, start, bufferSize - start, SocketFlags.Partial, callback, null);

            return completionSource.Task;
        }

        public static Task WriteBuffer(this Socket socket, JToken value)
        {
            return socket.WriteBuffer(value.ToBytesWithLengthPrefix());
        }

        public static Task WriteBuffer(this Socket socket, byte[] buffer)
        {
            var completionSource = new TaskCompletionSource<object>();
            var start = 0;
            AsyncCallback callback = null;
            callback = ar =>
            {
                int write;
                try
                {
                    write = socket.EndSend(ar);
                    start += write;
                }
                catch (Exception e)
                {
                    completionSource.SetException(e);
                    return;
                }
                if (start == buffer.Length)
                {
                    completionSource.SetResult(null);
                    return;
                }
                socket.BeginSend(buffer, start, buffer.Length - start, SocketFlags.None, callback, null);
            };
            socket.BeginSend(buffer, start, buffer.Length - start, SocketFlags.None, callback, null);

            return completionSource.Task;
        }
    }
}