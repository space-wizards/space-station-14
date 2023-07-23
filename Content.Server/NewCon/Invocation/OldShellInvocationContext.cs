using Content.Server.NewCon.Errors;
using Robust.Shared.Console;
using Robust.Shared.Players;
using Robust.Shared.Utility;

namespace Content.Server.NewCon.Invocation;

public sealed class OldShellInvocationContext : IInvocationContext
{
    public ICommonSession? Session => _shell.Player;

    private IConsoleShell _shell;
    private List<IConError> _errors = new();

    public void WriteLine(string line)
    {
        _shell.WriteLine(line);
    }

    public void WriteLine(FormattedMessage line)
    {
        _shell.WriteLine(line);
    }

    public void ReportError(IConError err)
    {
        _errors.Add(err);
    }

    public IEnumerable<IConError> GetErrors() => _errors;

    public void ClearErrors()
    {
        _errors.Clear();
    }

    public OldShellInvocationContext(IConsoleShell shell)
    {
        _shell = shell;
    }
}
