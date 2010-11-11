namespace Raven.Abstractions.Commands
{
    public enum CommandType
    {
        Consume = 1,
        Enqueue = 2,
        Read = 3,
        Reset = 4
    }
}