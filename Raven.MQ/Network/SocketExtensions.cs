using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace RavenMQ.Network
{
    public static class SocketExtensions
    {
        public static Task<Tuple<Socket, byte[], int>> ReadBuffer(this Socket socket, int bufferSize, byte[] separator)
        {
            var completionSource = new TaskCompletionSource<Tuple<Socket, byte[], int>>();
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
                if (read == 0 || 
                    start == bufferSize || 
                    ContainsSeparator(buffer, start, separator))
                {
                    completionSource.SetResult(Tuple.Create(socket, buffer, start));
                    return;
                }
                socket.BeginReceive(buffer, start, bufferSize - start, SocketFlags.None, callback, null);
            };
            socket.BeginReceive(buffer, start, bufferSize - start, SocketFlags.Partial, callback, null);

            return completionSource.Task;
        }

        private static bool ContainsSeparator(byte[] buffer, int start, byte[] separator)
        {
            for (int j = start-separator.Length; j >= separator.Length; j--)
            {
                var separatorStart = j;
                if (separator.Where((t, i) => buffer[separatorStart + i] != t).Any() == false)
                    return true;

            }
            return false;
        }

        public static Task<T> WriteBuffer<T>(this Socket socket, byte[] buffer, T result)
        {
            var completionSource = new TaskCompletionSource<T>();
            var start = 0;
            AsyncCallback callback = null;
            callback = ar =>
            {
                int read;
                try
                {
                    read = socket.EndSend(ar);
                    start += read;
                }
                catch (Exception e)
                {
                    completionSource.SetException(e);
                    return;
                }
                if (read == 0)
                {
                    completionSource.SetResult(result);
                }
                socket.BeginSend(buffer, start, buffer.Length - start, SocketFlags.None, callback, null);
            };
            socket.BeginSend(buffer, start, buffer.Length - start, SocketFlags.None, callback, null);

            return completionSource.Task;
        }
    }
}