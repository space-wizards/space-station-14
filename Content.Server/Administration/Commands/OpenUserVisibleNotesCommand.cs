using Content.Server.Administration.Notes;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AnyCommand]
public sealed class OpenUserVisibleNotesCommand : IConsoleCommand
{
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    public const string CommandName = "adminremarks";

    public string Command => CommandName;
    public string Description => Loc.GetString("admin-remarks-command-description");
    public string Help => $"Usage: {Command}";

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (!_configuration.GetCVar(CVars.SeeOwnNotes))
        {
            shell.WriteError(Loc.GetString("admin-remarks-command-error"));
            return;
        }

        if (shell.Player is not IPlayerSession player)
        {
            shell.WriteError("This does not work from the server console.");
            return;
        }

        await IoCManager.Resolve<IAdminNotesManager>().OpenUserNotesEui(player);
    }
}
