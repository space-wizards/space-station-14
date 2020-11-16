using System;
using System.Collections.Generic;
using Content.Shared.Administration;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    class DeleteEntitiesWithComponent : IClientCommand
    {
        public string Command => "deleteewc";
        public string Description
        {
            get
            {
                return Loc.GetString("Deletes entities with the specified components.");
            }
        }
        public string Help
        {
            get
            {
                return Loc.GetString("Usage: deleteewc <componentName_1> <componentName_2> ... <componentName_n>\nDeletes any entities with the components specified.");
            }
        }

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (args.Length < 1)
            {
                shell.SendText(player, Help);
                return;
            }

            var factory = IoCManager.Resolve<IComponentFactory>();

            var components = new List<Type>();
            foreach (var arg in args)
            {
                components.Add(factory.GetRegistration(arg).Type);
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();
            var entities = entityManager.GetEntities(new MultipleTypeEntityQuery(components));
            var count = 0;
            foreach (var entity in entities)
            {
                entity.Delete();
                count += 1;
            }

            shell.SendText(player, Loc.GetString("Deleted {0} entities", count));
        }
    }
}
