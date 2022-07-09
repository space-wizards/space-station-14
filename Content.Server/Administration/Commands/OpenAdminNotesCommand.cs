using Content.Server.Administration.Notes;
using Content.Server.Database;
using Content.Shared.Administration;
using Robust.Server.Player;
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
        if (shell.Player is not IPlayerSession player)
        {
            shell.WriteError("This does not work from the server console.");
            return;
        }

        Guid notedPlayer;

        switch (args.Length)
        {
            case 1 when Guid.TryParse(args[0], out notedPlayer):
                break;
            case 1:
                var db = IoCManager.Resolve<IServerDbManager>();
                var dbGuid = await db.GetAssignedUserIdAsync(args[0]);

                if (dbGuid == null)
                {
                    shell.WriteError($"Unable to find {args[0]} netuserid");
                    return;
                }

                notedPlayer = dbGuid.Value;
                break;
            default:
                shell.WriteError($"Invalid arguments.\n{Help}");
                return;
        }

        await IoCManager.Resolve<IAdminNotesManager>().OpenEui(player, notedPlayer);
    }
}
