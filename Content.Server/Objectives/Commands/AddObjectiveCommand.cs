using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.Objectives.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class AddObjectiveCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public string Command => "addobjective";
        public string Description => "Adds an objective to the player's mind.";
        public string Help => "addobjective <username> <objectiveID>";
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 2)
            {
                shell.WriteLine("Expected exactly 2 arguments.");
                return;
            }

            var mgr = IoCManager.Resolve<IPlayerManager>();
            if (!mgr.TryGetSessionByUsername(args[0], out var data))
            {
                shell.WriteLine("Can't find the playerdata.");
                return;
            }

            var minds = _entityManager.System<SharedMindSystem>();
            if (!minds.TryGetMind(data, out var mindId, out var mind))
            {
                shell.WriteLine("Can't find the mind.");
                return;
            }

            if (!IoCManager.Resolve<IPrototypeManager>()
                .TryIndex<EntityPrototype>(args[1], out var proto) ||
                !proto.TryGetComponent<ObjectiveComponent>(out _))
            {
                shell.WriteLine($"Can't find matching objective prototype {args[1]}");
                return;
            }

            if (!minds.TryAddObjective(mindId, mind, args[1]))
            {
                // can fail for other reasons so dont pretend to be right
                shell.WriteLine("Failed to add the objective. Maybe requirements dont allow that objective to be added.");
            }
        }
    }
}
