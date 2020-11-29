#nullable enable
using System;
using System.Collections.Generic;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Commands
{
    [AdminCommand(AdminFlags.Mapping)]
    public class FindEntitiesWithComponents : IClientCommand
    {
        public string Command => "findentitieswithcomponents";
        public string Description => "Finds entities with all of the specified components.";
        public string Help => $"{Command} <componentName1> <componentName2>...";

        public void Execute(IConsoleShell shell, IPlayerSession? player, string[] args)
        {
            if (args.Length == 0)
            {
                shell.SendText(player, $"Invalid amount of arguments: {args.Length}.\n{Help}");
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
                shell.SendText(player, $"No component found for component names: {string.Join(", ", invalidArgs)}");
                return;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();
            var query = new MultipleTypeEntityQuery(components);
            var entityIds = new HashSet<string>();

            foreach (var entity in entityManager.GetEntities(query))
            {
                if (entity.Prototype == null)
                {
                    continue;
                }

                entityIds.Add(entity.Prototype.ID);
            }

            if (entityIds.Count == 0)
            {
                shell.SendText(player, $"No entities found with components {string.Join(", ", args)}.");
                return;
            }

            shell.SendText(player, $"{entityIds.Count} entities found:\n{string.Join("\n", entityIds)}");
        }
    }
}
