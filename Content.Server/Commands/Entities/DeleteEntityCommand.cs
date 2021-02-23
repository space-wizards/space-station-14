using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Commands.Entities
{
    [AdminCommand(AdminFlags.Spawn)]
    public class DeleteEntityCommand : IConsoleCommand
    {
        public string Command => "deleteentity";
        public string Description => "Deletes an entity with the given id.";
        public string Help => $"Usage: {Command} <id>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteLine($"Invalid amount of arguments.\n{Help}");
                return;
            }

            if (!EntityUid.TryParse(args[0], out var id))
            {
                shell.WriteLine($"{args[0]} is not a valid entity id.");
                return;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();

            if (!entityManager.TryGetEntity(id, out var entity))
            {
                shell.WriteLine($"No entity found with id {id}.");
                return;
            }

            entity.Delete();
            shell.WriteLine($"Deleted entity with id {id}.");
        }
    }
}
