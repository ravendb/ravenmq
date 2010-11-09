using System;

namespace RavenMQ.Impl
{
    public interface IUuidGenerator
    {
        Guid CreateSequentialUuid();
    }
}