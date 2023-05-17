using Robust.Client.Console;

namespace Content.Replay;

public sealed class ConGroup : IClientConGroupImplementation
{
    public event Action? ConGroupUpdated;
    public bool CanAdminMenu() => true;
    public bool CanAdminPlace() => true;
    public bool CanCommand(string cmdName) => true;
    public bool CanScript() => true;
    public bool CanViewVar() => true;
}
