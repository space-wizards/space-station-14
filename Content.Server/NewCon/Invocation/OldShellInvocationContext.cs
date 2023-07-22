using Robust.Shared.Console;
using Robust.Shared.Players;

namespace Content.Server.NewCon.Invocation;

public sealed class OldShellInvocationContext : IInvocationContext
{
    public ICommonSession? Session => _shell.Player;

    private IConsoleShell _shell;

    public void WriteLine(string line)
    {
        _shell.WriteLine(line);
    }

    public OldShellInvocationContext(IConsoleShell shell)
    {
        _shell = shell;
    }
}
