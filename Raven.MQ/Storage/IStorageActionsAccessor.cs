namespace RavenMQ.Storage
{
    public interface IStorageActionsAccessor
    {
        GeneralStorageActions General { get; }
        MessagesActions Messages { get; }
    }
}