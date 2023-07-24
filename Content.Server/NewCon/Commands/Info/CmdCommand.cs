using System.Linq;

namespace Content.Server.NewCon.Commands.Info;

[ConsoleCommand]
public sealed class CmdCommand : ConsoleCommand
{
    [CommandImplementation("list")]
    public IEnumerable<ConsoleCommand> List()
    {
        return ConManager.AllCommands().Select(x => x.Item1);
    }

    [CommandImplementation("explain")]
    public void Explain([CommandInvocationContext] IInvocationContext ctx)
    {

    }
}
