using System;
using System.Collections.Generic;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Administration
{
    class DeleteEntitiesWithComponent : IClientCommand
    {
        public string Command => "deleteewc";
        public string Description
        {
            get
            {
                var localizationManager = IoCManager.Resolve<ILocalizationManager>();
                return localizationManager.GetString("Deletes entities with the specified components.");
            }
        }
        public string Help
        {
            get
            {
                var localizationManager = IoCManager.Resolve<ILocalizationManager>();
                return localizationManager.GetString("Usage: deleteewc <componentName_1> <componentName_2> ... <componentName_n>\nDeletes any entities with the components specified.");
            }
        }

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            var localizationManager = IoCManager.Resolve<ILocalizationManager>();
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

            shell.SendText(player, localizationManager.GetString("Deleted {0} entities", count));
        }
    }
}
