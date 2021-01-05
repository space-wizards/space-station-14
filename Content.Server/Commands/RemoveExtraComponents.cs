#nullable enable
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Server.Commands
{
    [AdminCommand(AdminFlags.Mapping)]
    public class RemoveExtraComponents : IClientCommand
    {
        public string Command => "removeextracomponents";
        public string Description => "Removes all components from all entities of the specified id if that component is not in its prototype.\nIf no id is specified, it matches all entities.";
        public string Help => $"{Command} <entityId> / {Command}";
        public void Execute(IConsoleShell shell, IPlayerSession? player, string[] args)
        {
            var id = args.Length == 0 ? null : string.Join(" ", args);
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

            IEntityQuery query;

            if (id == null)
            {
                query = new AllEntityQuery();
            }
            else
            {
                if (!prototypeManager.TryIndex(id, out EntityPrototype prototype))
                {
                    shell.SendText(player, $"No entity prototype found with id {id}.");
                    return;
                }

                query = new PredicateEntityQuery(e => e.Prototype == prototype);
            }

            var entities = 0;
            var components = 0;

            foreach (var entity in entityManager.GetEntities(query))
            {
                if (entity.Prototype == null)
                {
                    continue;
                }

                var modified = false;

                foreach (var component in entity.GetAllComponents())
                {
                    if (!entity.Prototype.Components.ContainsKey(component.Name))
                    {
                        entityManager.ComponentManager.RemoveComponent(entity.Uid, component);
                        components++;

                        modified = true;
                    }
                }

                if (modified)
                {
                    entities++;
                }
            }

            shell.SendText(player, $"Removed {components} components from {entities} entities{(id == null ? "." : $" with id {id}")}");
        }
    }
}
