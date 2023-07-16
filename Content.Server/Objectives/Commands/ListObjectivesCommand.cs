using System.Linq;
using Content.Server.Administration;
using Content.Server.Players;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Objectives.Commands
{
    [AdminCommand(AdminFlags.Logs)]
    public sealed class ListObjectivesCommand : IConsoleCommand
    {
        public string Command => "lsobjectives";
        public string Description => "Lists all objectives in a players mind.";
        public string Help => "lsobjectives [<username>]";
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            IPlayerData? data;
            if (args.Length == 0 && player != null)
            {
                data = player.Data;
            }
            else if (player == null || !IoCManager.Resolve<IPlayerManager>().TryGetPlayerDataByUsername(args[0], out data))
            {
                shell.WriteLine("Can't find the playerdata.");
                return;
            }

            var mind = data.ContentData()?.Mind;
            if (mind == null)
            {
                shell.WriteLine("Can't find the mind.");
                return;
            }

            shell.WriteLine($"Objectives for player {data.UserId}:");
            var objectives = mind.AllObjectives.ToList();
            if (objectives.Count == 0)
            {
                shell.WriteLine("None.");
            }
            for (var i = 0; i < objectives.Count; i++)
            {
                shell.WriteLine($"- [{i}] {objectives[i].Conditions[0].Title}");
            }

        }
    }
}
