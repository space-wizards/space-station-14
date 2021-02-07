#nullable enable
using Content.Server.Administration;
using Content.Server.GameObjects.Components.Movement;
using Content.Server.GameObjects.EntitySystems.AI;
using Content.Shared.Administration;
using Content.Shared.GameObjects.Components.Movement;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Commands.AI
{
    [AdminCommand(AdminFlags.Fun)]
    public class AddAiCommand : IConsoleCommand
    {
        public string Command => "addai";
        public string Description => "Add an ai component with a given processor to an entity.";
        public string Help => "Usage: addai <processorId> <entityId>"
                              + "\n    processorId: Class that inherits AiLogicProcessor and has an AiLogicProcessor attribute."
                              + "\n    entityID: Uid of entity to add the AiControllerComponent to. Open its VV menu to find this.";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if(args.Length != 2)
            {
                shell.WriteLine("Wrong number of args.");
                return;
            }

            var processorId = args[0];
            var entId = new EntityUid(int.Parse(args[1]));
            var ent = IoCManager.Resolve<IEntityManager>().GetEntity(entId);
            var aiSystem = EntitySystem.Get<AiSystem>();

            if (!aiSystem.ProcessorTypeExists(processorId))
            {
                shell.WriteLine("Invalid processor type. Processor must inherit AiLogicProcessor and have an AiLogicProcessor attribute.");
                return;
            }
            if (ent.HasComponent<AiControllerComponent>())
            {
                shell.WriteLine("Entity already has an AI component.");
                return;
            }

            if (ent.HasComponent<IMoverComponent>())
            {
                ent.RemoveComponent<IMoverComponent>();
            }

            var comp = ent.AddComponent<AiControllerComponent>();
            comp.LogicName = processorId;
            shell.WriteLine("AI component added.");
        }
    }
}
