using System.Linq;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Mapping)]
    public sealed class FindEntitiesWithComponents : IConsoleCommand
    {
        public string Command => "findentitieswithcomponents";
        public string Description => "Finds entities with all of the specified components.";
        public string Help => $"{Command} <componentName1> <componentName2>...";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length == 0)
            {
                shell.WriteLine($"Invalid amount of arguments: {args.Length}.\n{Help}");
                return;
            }

            var components = new List<Type>();
            var componentFactory = IoCManager.Resolve<IComponentFactory>();
            var invalidArgs = new List<string>();

            foreach (var arg in args)
            {
                if (!componentFactory.TryGetRegistration(arg, out var registration))
                {
                    invalidArgs.Add(arg);
                    continue;
                }

                components.Add(registration.Type);
            }

            if (invalidArgs.Count > 0)
            {
                shell.WriteLine($"No component found for component names: {string.Join(", ", invalidArgs)}");
                return;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();
            var entityIds = new HashSet<string>();

            var entitiesWithComponents = components.Select(c => entityManager.GetAllComponents(c).Select(x => x.Owner)).ToArray();
            var entitiesWithAllComponents = entitiesWithComponents.Skip(1).Aggregate(new HashSet<EntityUid>(entitiesWithComponents.First()), (h, e) => { h.IntersectWith(e); return h; });

            foreach (var entity in entitiesWithAllComponents)
            {
                if (entityManager.GetComponent<MetaDataComponent>(entity).EntityPrototype is not { } prototypeId)
                {
                    continue;
                }

                entityIds.Add(prototypeId.ID);
            }

            if (entityIds.Count == 0)
            {
                shell.WriteLine($"No entities found with components {string.Join(", ", args)}.");
                return;
            }

            shell.WriteLine($"{entityIds.Count} entities found:\n{string.Join("\n", entityIds)}");
        }
    }
}
