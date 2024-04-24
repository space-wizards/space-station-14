using Content.Server.Administration;
using Content.Server.NPC.HTN;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.NPC.Commands
{
    [AdminCommand(AdminFlags.Fun)]
    public sealed class AddNPCCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entities = default!;

        public string Command => "addnpc";
        public string Description => "Add a HTN NPC component with a given root task";
        public string Help => "Usage: addnpc <entityId> <rootTask>"
                              + "\n    entityID: Uid of entity to add the AiControllerComponent to. Open its VV menu to find this."
                              + "\n    rootTask: Name of a behaviorset to add to the component on initialize.";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 2)
            {
                shell.WriteError("Wrong number of args.");
                return;
            }

            var nent = new NetEntity(int.Parse(args[0]));

            if (!_entities.TryGetEntity(nent, out var entId))
            {
                shell.WriteError($"Unable to find entity {nent}");
                return;
            }

            if (_entities.HasComponent<HTNComponent>(entId))
            {
                shell.WriteError("Entity already has an NPC component.");
                return;
            }

            var comp = _entities.AddComponent<HTNComponent>(entId.Value);
            comp.RootTask = new HTNCompoundTask()
            {
                Task = args[1]
            };
            shell.WriteLine("AI component added.");
        }
    }
}
