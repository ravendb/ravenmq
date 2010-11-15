using System;
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
                    EndsWithSeparator(buffer, start, separator))
                {
                    completionSource.SetResult(Tuple.Create(socket, buffer, start));
                    return;
                }
                socket.BeginReceive(buffer, start, bufferSize - start, SocketFlags.None, callback, null);
            };
            socket.BeginReceive(buffer, start, bufferSize - start, SocketFlags.Partial, callback, null);

            return completionSource.Task;
        }

        private static bool EndsWithSeparator(byte[] buffer, int start, byte[] separator)
        {
            if (separator.Length >= start)
                return false;
            var separatorStart = start - separator.Length;
            for (int i = 0; i < separator.Length; i++)
            {
                if (buffer[separatorStart + i] != separator[i])
                    return false;
            }
            return true;
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