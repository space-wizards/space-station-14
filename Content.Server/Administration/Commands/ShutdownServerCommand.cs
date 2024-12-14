using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Server)]

public sealed class ShutdownServerCommand : IConsoleCommand
{
    public string Command => "shutdownserver";
    public string Description => Loc.GetString("Immediately shuts down the server");
    public string Help => Loc.GetString("shutdownserver off server", ("command",Command));

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        shell.RemoteExecuteCommand("quit");
    }
}
