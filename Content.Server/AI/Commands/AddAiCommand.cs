using Content.Server.Administration;
using Content.Server.AI.Components;
using Content.Server.AI.EntitySystems;
using Content.Server.AI.Utility;
using Content.Server.AI.Utility.AiLogic;
using Content.Shared.Administration;
using Content.Shared.Movement.Components;
using Robust.Shared.Console;

namespace Content.Server.AI.Commands
{
    [AdminCommand(AdminFlags.Fun)]
    public sealed class AddAiCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entities = default!;

        public string Command => "addai";
        public string Description => "Add an ai component with a given processor to an entity.";
        public string Help => "Usage: addai <entityId> <behaviorSet1> <behaviorSet2>..."
                              + "\n    entityID: Uid of entity to add the AiControllerComponent to. Open its VV menu to find this."
                              + "\n    behaviorSet: Name of a behaviorset to add to the component on initialize.";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if(args.Length < 1)
            {
                shell.WriteLine("Wrong number of args.");
                return;
            }

            var entId = new EntityUid(int.Parse(args[0]));

            if (!_entities.EntityExists(entId))
            {
                shell.WriteLine($"Unable to find entity with uid {entId}");
                return;
            }

            if (_entities.HasComponent<NPCComponent>(entId))
            {
                shell.WriteLine("Entity already has an AI component.");
                return;
            }

            var comp = _entities.AddComponent<UtilityNPCComponent>(entId);
            var npcSystem = IoCManager.Resolve<IEntityManager>().EntitySysManager.GetEntitySystem<NPCSystem>();

            for (var i = 1; i < args.Length; i++)
            {
                var bSet = args[i];
                npcSystem.AddBehaviorSet(comp, bSet, false);
            }

            npcSystem.RebuildActions(comp);
            shell.WriteLine("AI component added.");
        }
    }
}
