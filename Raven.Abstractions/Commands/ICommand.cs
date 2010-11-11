
namespace Raven.Abstractions.Commands
{
    public interface ICommand
    {
        CommandType Type { get; }
    }
}