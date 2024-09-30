using Robust.Client.Console;

namespace Content.Client.Replay;

public sealed class ReplayConGroup : IClientConGroupImplementation
{
    public event Action? ConGroupUpdated { add { } remove { } }
    public bool CanAdminMenu() => true;
    public bool CanAdminPlace() => true;
    public bool CanCommand(string cmdName) => true;
    public bool CanScript() => true;
    public bool CanViewVar() => true;
}
