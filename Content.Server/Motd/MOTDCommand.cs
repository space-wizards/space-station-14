using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Motd;

/// <summary>
/// A console command which acts as an alias for <see cref="GetMotdCommand"/> or <see cref="SetMotdCommand"/> depending on the number of arguments given.
/// </summary>
[AnyCommand]
internal sealed class MOTDCommand : LocalizedCommands
{
    [Dependency] private readonly IAdminManager _adminManager = default!;

    public override string Command => "motd";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player;
        if (args.Length < 1 || (player != null && _adminManager is AdminManager aMan && !aMan.CanCommand(player, "set-motd")))
            shell.ConsoleHost.ExecuteCommand(shell.Player, "get-motd");
        else
            shell.ConsoleHost.ExecuteCommand(shell.Player, $"set-motd {string.Join(" ", args)}");
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        var player = shell.Player;
        if (player != null && _adminManager is AdminManager aMan && !aMan.CanCommand(player, "set-motd"))
            return CompletionResult.Empty;
        if (args.Length == 1)
            return CompletionResult.FromHint(Loc.GetString("cmd-set-motd-hint-head"));
        return CompletionResult.FromHint(Loc.GetString("cmd-set-motd-hint-cont"));
    }
}
