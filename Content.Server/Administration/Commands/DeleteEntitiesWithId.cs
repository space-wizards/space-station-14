using System.Linq;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Spawn)]
    public sealed class DeleteEntitiesWithId : IConsoleCommand
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
            var entities = entityManager.GetEntities().Where(e => entityManager.GetComponent<MetaDataComponent>(e).EntityPrototype?.ID.ToLower() == id);
            var i = 0;

            foreach (var entity in entities)
            {
                entityManager.DeleteEntity(entity);
                i++;
            }

            shell.WriteLine($"Deleted all entities with id {id}. Occurrences: {i}");
        }
    }
}
