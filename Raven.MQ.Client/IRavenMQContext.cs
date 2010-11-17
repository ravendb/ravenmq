using System.Threading.Tasks;
using Raven.Abstractions.Data;

namespace Raven.MQ.Client
{
    public interface IRavenMQContext
    {
        void Send(IncomingMessage msg);

        Task FlushAsync();
    }
}