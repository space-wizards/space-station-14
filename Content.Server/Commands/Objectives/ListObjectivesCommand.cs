#nullable enable
using System.Linq;
using Content.Server.Administration;
using Content.Server.Players;
using Content.Shared.Administration;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.IoC;

namespace Content.Server.Commands.Objectives
{
    [AdminCommand(AdminFlags.Admin)]
    public class ListObjectivesCommand : IClientCommand
    {
        public string Command => "lsobjectives";
        public string Description => "Lists all objectives in a players mind.";
        public string Help => "lsobjectives [<username>]";
        public void Execute(IConsoleShell shell, IPlayerSession? player, string[] args)
        {
            IPlayerData? data;
            if (args.Length == 0 && player != null)
            {
                data = player.Data;
            }
            else if (player == null || !IoCManager.Resolve<IPlayerManager>().TryGetPlayerDataByUsername(args[0], out data))
            {
                shell.SendText(player, "Can't find the playerdata.");
                return;
            }

            var mind = data.ContentData()?.Mind;
            if (mind == null)
            {
                shell.SendText(player, "Can't find the mind.");
                return;
            }

            shell.SendText(player, $"Objectives for player {data.UserId}:");
            var objectives = mind.AllObjectives.ToList();
            if (objectives.Count == 0)
            {
                shell.SendText(player, "None.");
            }
            for (var i = 0; i < objectives.Count; i++)
            {
                shell.SendText(player, $"- [{i}] {objectives[i]}");
            }

        }
    }
}
