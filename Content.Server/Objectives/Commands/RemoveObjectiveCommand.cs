using Content.Server.Administration;
using Content.Server.Mind;
using Content.Server.Players;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Objectives.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class RemoveObjectiveCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        
        public string Command => "rmobjective";
        public string Description => "Removes an objective from the player's mind.";
        public string Help => "rmobjective <username> <index>";
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 2)
            {
                shell.WriteLine("Expected exactly 2 arguments.");
                return;
            }

            var mgr = IoCManager.Resolve<IPlayerManager>();
            if (mgr.TryGetPlayerDataByUsername(args[0], out var data))
            {
                var mind = data.ContentData()?.Mind;
                if (mind == null)
                {
                    shell.WriteLine("Can't find the mind.");
                    return;
                }

                if (int.TryParse(args[1], out var i))
                {
                    var mindSystem = _entityManager.System<MindSystem>();
                    shell.WriteLine(mindSystem.TryRemoveObjective(mind, i)
                        ? "Objective successfully removed!"
                        : "Objective removing failed. Maybe the index is out of bounds? Check lsobjectives!");
                }
                else
                {
                    shell.WriteLine($"Invalid index {args[1]}!");
                }
            }
            else
            {
                shell.WriteLine("Can't find the playerdata.");
            }
        }
    }
}
