using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Motd;

/// <summary>
/// A console command which acts as an alias for <see cref="GetMotdCommand"/> or <see cref="SetMotdCommand"/> depending on the number of arguments given.
/// </summary>
[AnyCommand]
internal sealed class MOTDCommand : LocalizedCommands
{
    public override string Command => "motd";
    
    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1)
            shell.ConsoleHost.ExecuteCommand(shell.Player, "get-motd");
        else
            shell.ConsoleHost.ExecuteCommand(shell.Player, $"set-motd {string.Join(" ", args)}");
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length < 1)
            return CompletionResult.Empty;
        return CompletionResult.FromHint(Loc.GetString("cmd-set-motd-hint"));
    }
}
