using System.Text;
using Content.Server.Database;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Ban)]
    public sealed class PardonCommand : IConsoleCommand
    {
        public string Command => "pardon";
        public string Description => "Pardons somebody's ban";
        public string Help => $"Usage: {Command} <ban id>";

        public async void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player;
            var dbMan = IoCManager.Resolve<IServerDbManager>();

            if (args.Length != 1)
            {
                shell.WriteLine(Help);
                return;
            }

            if (!int.TryParse(args[0], out var banId))
            {
                shell.WriteLine($"Unable to parse {args[0]} as a ban id integer.\n{Help}");
                return;
            }

            var ban = await dbMan.GetServerBanAsync(banId);

            if (ban == null)
            {
                shell.WriteLine($"No ban found with id {banId}");
                return;
            }

            if (ban.Unban != null)
            {
                var response = new StringBuilder("This ban has already been pardoned");

                if (ban.Unban.UnbanningAdmin != null)
                {
                    response.Append($" by {ban.Unban.UnbanningAdmin.Value}");
                }

                response.Append($" in {ban.Unban.UnbanTime}.");

                shell.WriteLine(response.ToString());
                return;
            }

            await dbMan.AddServerUnbanAsync(new ServerUnbanDef(banId, player?.UserId, DateTimeOffset.Now));

            shell.WriteLine($"Pardoned ban with id {banId}");
        }
    }
}
