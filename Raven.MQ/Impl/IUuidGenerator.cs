using System;

namespace RavenMQ.Storage
{
    public interface IUuidGenerator
    {
        Guid CreateSequentialUuid();
    }
}