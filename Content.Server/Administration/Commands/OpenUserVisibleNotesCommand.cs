using Content.Server.Administration.Notes;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AnyCommand]
public sealed partial class OpenUserVisibleNotesCommand : IConsoleCommand
{
    [Dependency] private IConfigurationManager _configuration = default!;
    [Dependency] private IAdminNotesManager _notes = default!;

    public const string CommandName = "adminremarks";

    public string Command => CommandName;
    public string Description => Loc.GetString("admin-remarks-command-description");
    public string Help => $"Usage: {Command}";

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (!_configuration.GetCVar(CCVars.SeeOwnNotes))
        {
            shell.WriteError(Loc.GetString("admin-remarks-command-error"));
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
