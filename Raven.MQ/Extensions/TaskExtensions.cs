using System;
using System.Threading.Tasks;

namespace RavenMQ.Extensions
{
    public static class TaskExtensions
    {
        public static Task IgnoreExceptions(this Task task)
        {
            task.ContinueWith(c => GC.KeepAlive(c.Exception),
                TaskContinuationOptions.ExecuteSynchronously);
            return task;
        }

        public static Task<T> IgnoreExceptions<T>(this Task<T> task)
        {
            task.ContinueWith(c => GC.KeepAlive(c.Exception),
                TaskContinuationOptions.ExecuteSynchronously);
            return task;
        }
    }
}