using Content.Server.Objectives.Interfaces;
using Content.Server.Players;
using Content.Shared.Roles;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Server.Objectives
{
    public class AddObjectiveCommand : IClientCommand
    {
        [Dependency] private readonly IObjectivesManager _objectivesManager = default!;

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
            if (mgr.TryGetPlayerDataByUsername(args[0], out var data))
            {
                var mind = data.ContentData()?.Mind;
                if (mind == null)
                {
                    shell.SendText(player, "Can't find the mind.");
                }

                if (!mind.TryAddObjective(args[1], out var _))
                {
                     shell.SendText(player, "Objective either doesn't exist or cannot be added.");
                }
            }
            else
            {
                shell.SendText(player, "Can't find the playerdata.");
            }
        }
    }

    public class RemoveObjectiveCommand : IClientCommand
    {
        public string Command => "rmobjective";
        public string Description => "Removes an objective from the player's mind.";
        public string Help => "rmobjective <username> TBD";
        public void Execute(IConsoleShell shell, IPlayerSession? player, string[] args)
        {
            throw new System.NotImplementedException();
        }
    }
}
