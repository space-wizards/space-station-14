using System.Linq;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Spawn)]
    sealed class DeleteEntitiesWithComponent : IConsoleCommand
    {
        public string Command => "deleteewc";

        public string Description => Loc.GetString("delete-entities-with-component-command-description");

        public string Help => Loc.GetString("delete-entities-with-component-command-help-text");

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 1)
            {
                shell.WriteLine(Help);
                return;
            }

            var factory = IoCManager.Resolve<IComponentFactory>();

            var components = new List<Type>();
            foreach (var arg in args)
            {
                components.Add(factory.GetRegistration(arg).Type);
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();

            var entitiesWithComponents = components.Select(c => entityManager.GetAllComponents(c).Select(x => x.Owner));
            var entitiesWithAllComponents = entitiesWithComponents.Skip(1).Aggregate(new HashSet<EntityUid>(entitiesWithComponents.First()), (h, e) => { h.IntersectWith(e); return h; });

            var count = 0;
            foreach (var entity in entitiesWithAllComponents)
            {
                entityManager.DeleteEntity(entity);
                count += 1;
            }

            shell.WriteLine(Loc.GetString("delete-entities-with-component-command-deleted-components",("count", count)));
        }
    }
}
