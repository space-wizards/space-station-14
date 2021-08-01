using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using System.Linq;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public class DeleteEntitiesWithId : IConsoleCommand
    {
        public string Command => "deleteewi";
        public string Description => "Deletes entities with the specified prototype ID.";
        public string Help => $"Usage: {Command} <prototypeID>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteLine(Help);
                return;
            }

            var id = args[0].ToLower();
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var entities = entityManager.GetEntities().Where(e => e.Prototype?.ID.ToLower() == id);
            var i = 0;

            foreach (var entity in entities)
            {
                entity.Delete();
                i++;
            }

            shell.WriteLine($"Deleted all entities with id {id}. Occurrences: {i}");
        }
    }
}
