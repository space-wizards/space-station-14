using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Mapping)]
    public class RemoveExtraComponents : IConsoleCommand
    {
        public string Command => "removeextracomponents";
        public string Description => "Removes all components from all entities of the specified id if that component is not in its prototype.\nIf no id is specified, it matches all entities.";
        public string Help => $"{Command} <entityId> / {Command}";
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var id = args.Length == 0 ? null : string.Join(" ", args);
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

            EntityPrototype? prototype = null;
            var checkPrototype = !string.IsNullOrEmpty(id);

            if (checkPrototype && !prototypeManager.TryIndex(id!, out prototype))
            {
                shell.WriteError($"Can't find entity prototype with id \"{id}\"!");
                return;
            }

            var entities = 0;
            var components = 0;

            foreach (var entity in entityManager.GetEntities())
            {
                if (checkPrototype && entity.Prototype != prototype || entity.Prototype == null)
                {
                    continue;
                }

                var modified = false;

                foreach (var component in entity.GetAllComponents())
                {
                    if (entity.Prototype.Components.ContainsKey(component.Name))
                        continue;

                    entityManager.RemoveComponent(entity.Uid, component);
                    components++;

                    modified = true;
                }

                if (modified)
                    entities++;
            }

            shell.WriteLine($"Removed {components} components from {entities} entities{(id == null ? "." : $" with id {id}")}");
        }
    }
}
