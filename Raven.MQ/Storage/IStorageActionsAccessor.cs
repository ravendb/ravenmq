namespace RavenMQ.Storage
{
    public interface IStorageActionsAccessor
    {
        MessagesActions Messages { get; }
    }
}