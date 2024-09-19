using Content.Server.Administration.Notes;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.ViewNotes)]
public sealed class OpenAdminNotesCommand : IConsoleCommand
{
    public const string CommandName = "adminnotes";

    public string Command => CommandName;
    public string Description => "Opens the admin notes panel.";
    public string Help => $"Usage: {Command} <notedPlayerUserId OR notedPlayerUsername>";

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        Guid notedPlayer;

        switch (args.Length)
        {
            case 1 when Guid.TryParse(args[0], out notedPlayer):
                break;
            case 1:
                var locator = IoCManager.Resolve<IPlayerLocator>();
                var dbGuid = await locator.LookupIdByNameAsync(args[0]);

                if (dbGuid == null)
                {
                    shell.WriteError($"Unable to find {args[0]} netuserid");
                    return;
                }

                notedPlayer = dbGuid.UserId;
                break;
            default:
                shell.WriteError($"Invalid arguments.\n{Help}");
                return;
        }

        await IoCManager.Resolve<IAdminNotesManager>().OpenEui(player, notedPlayer);
    }
}
