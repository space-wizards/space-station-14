#nullable enable
using System.Linq;
using Content.Server.Administration;
using Content.Server.Players;
using Content.Shared.Administration;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Server.Objectives
{
    [AdminCommand(AdminFlags.Admin)]
    public class AddObjectiveCommand : IClientCommand
    {
        public string Command => "addobjective";
        public string Description => "Adds an objective to the player's mind.";
        public string Help => "addobjective <username> <objectiveID>";
        public void Execute(IConsoleShell shell, IPlayerSession? player, string[] args)
        {
            if (args.Length != 2)
            {
                shell.SendText(player, "Expected exactly 2 arguments.");
                return;
            }

            var mgr = IoCManager.Resolve<IPlayerManager>();
            if (!mgr.TryGetPlayerDataByUsername(args[0], out var data))
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

            if (!IoCManager.Resolve<IPrototypeManager>()
                .TryIndex<ObjectivePrototype>(args[1], out var objectivePrototype))
            {
                shell.SendText(player, $"Can't find matching ObjectivePrototype {objectivePrototype}");
                return;
            }

            if (!mind.TryAddObjective(objectivePrototype))
            {
                shell.SendText(player, "Objective requirements dont allow that objective to be added.");
            }

        }
    }

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

    [AdminCommand(AdminFlags.Admin)]
    public class RemoveObjectiveCommand : IClientCommand
    {
        public string Command => "rmobjective";
        public string Description => "Removes an objective from the player's mind.";
        public string Help => "rmobjective <username> <index>";
        public void Execute(IConsoleShell shell, IPlayerSession? player, string[] args)
        {
            if (args.Length != 2)
            {
                shell.SendText(player, "Expected exactly 2 arguments.");
                return;
            }

            var mgr = IoCManager.Resolve<IPlayerManager>();
            if (mgr.TryGetPlayerDataByUsername(args[0], out var data))
            {
                var mind = data.ContentData()?.Mind;
                if (mind == null)
                {
                    shell.SendText(player, "Can't find the mind.");
                    return;
                }

                if (int.TryParse(args[1], out var i))
                {
                    shell.SendText(player,
                        mind.TryRemoveObjective(i)
                            ? "Objective successfully removed!"
                            : "Objective removing failed. Maybe the index is out of bounds? Check lsobjectives!");
                }
                else
                {
                    shell.SendText(player, $"Invalid index {args[1]}!");
                }
            }
            else
            {
                shell.SendText(player, "Can't find the playerdata.");
            }
        }
    }
}
