
namespace Raven.Abstractions.Commands
{
    public interface ICommand
    {
    	string ArgumentsForLog { get; }
        CommandType Type { get; }
    }
}