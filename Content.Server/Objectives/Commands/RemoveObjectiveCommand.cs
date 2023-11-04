using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Mind;
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
            var minds = _entityManager.System<SharedMindSystem>();
            if (!mgr.TryGetSessionByUsername(args[0], out var session))
            {
                shell.WriteLine("Can't find the playerdata.");
                return;
            }

            if (!minds.TryGetMind(session, out var mindId, out var mind))
            {
                shell.WriteLine("Can't find the mind.");
                return;
            }

            if (int.TryParse(args[1], out var i))
            {
                var mindSystem = _entityManager.System<SharedMindSystem>();
                shell.WriteLine(mindSystem.TryRemoveObjective(mindId, mind, i)
                    ? "Objective successfully removed!"
                    : "Objective removing failed. Maybe the index is out of bounds? Check lsobjectives!");
            }
            else
            {
                shell.WriteLine($"Invalid index {args[1]}!");
            }
        }
    }
}
