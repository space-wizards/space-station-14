#nullable enable
using Content.Shared.Administration;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public class DeleteEntitiesWithId : IClientCommand
    {
        public string Command => "deleteewi";
        public string Description => "Deletes entities with the specified prototype ID.";
        public string Help => $"Usage: {Command} <prototypeID>";

        public void Execute(IConsoleShell shell, IPlayerSession? player, string[] args)
        {
            if (args.Length != 1)
            {
                shell.SendText(player, Help);
                return;
            }

            var id = args[0].ToLower();
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var query = new PredicateEntityQuery(e => e.Prototype?.ID.ToLower() == id);
            var entities = entityManager.GetEntities(query);
            var i = 0;

            foreach (var entity in entities)
            {
                entity.Delete();
                i++;
            }

            shell.SendText(player, $"Deleted all entities with id {id}. Occurrences: {i}");
        }
    }
}
