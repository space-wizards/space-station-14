using Content.Server.Administration.Notes;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AnyCommand]
public sealed class OpenUserVisibleNotesCommand : LocalizedCommands
{
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IAdminNotesManager _notes = default!;

    public override string Command => "adminremarks";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (!_configuration.GetCVar(CCVars.SeeOwnNotes))
        {
            shell.WriteError(Loc.GetString("cmd-adminremarks-error"));
            return;
        }

        if (shell.Player is not { } player)
        {
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        await _notes.OpenUserNotesEui(player);
    }
}
