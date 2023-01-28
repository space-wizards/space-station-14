using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Server.Commands;
using Content.Shared.Database;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Console;

namespace Content.Server.Motd;

/// <summary>
/// A console command which acts as an alias for <see cref="GetMotdCommand"/> or <see cref="SetMotdCommand"/> depending on the number of arguments given.
/// </summary>
[AnyCommand]
internal sealed class MOTDCommand : IConsoleCommand
{
    [Dependency] private readonly IConsoleHost _consoleHost = default!;

    public string Command => "motd";
    public string Description => "Print or set the Message Of The Day.";
    public string Help => "motd [ <text> ... ]";
    
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1)
            shell.ConsoleHost.ExecuteCommand(shell.Player, "get-motd");
        else
            shell.ConsoleHost.ExecuteCommand(shell.Player, $"set-motd {string.Join(" ", args)}");
    }
}
