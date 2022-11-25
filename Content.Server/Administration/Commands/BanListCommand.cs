using Content.Server.Administration.BanList;
using Content.Server.EUI;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Ban)]
    public sealed class BanListCommand : IConsoleCommand
    {
        public string Command => "banlist";
        public string Description => "Opens the ban list panel.";
        public string Help => $"Usage: {Command} <userid or username>";

        public async void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (shell.Player is not IPlayerSession player)
            {
                shell.WriteError("This does not work from the server console.");
                return;
            }

            Guid banListPlayer;

            switch (args.Length)
            {
                case 1 when Guid.TryParse(args[0], out banListPlayer):
                    break;
                case 1:
                    var locator = IoCManager.Resolve<IPlayerLocator>();
                    var dbGuid = await locator.LookupIdByNameAsync(args[0]);

                    if (dbGuid == null)
                    {
                        shell.WriteError($"Unable to find {args[0]} netuserid");
                        return;
                    }

                    banListPlayer = dbGuid.UserId;
                    break;
                default:
                    shell.WriteError($"Invalid arguments.\n{Help}");
                    return;
            }

            var euis = IoCManager.Resolve<EuiManager>();
            var ui = new BanListEui();
            euis.OpenEui(ui, player);
            await ui.ChangeBanListPlayer(banListPlayer);
        }
    }
}
