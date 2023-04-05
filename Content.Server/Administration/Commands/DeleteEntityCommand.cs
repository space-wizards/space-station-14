using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Spawn)]
    public sealed class DeleteEntityCommand : IConsoleCommand
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

            if (!entityManager.EntityExists(id))
            {
                shell.WriteLine($"No entity found with id {id}.");
                return;
            }

            entityManager.DeleteEntity(id);
            shell.WriteLine($"Deleted entity with id {id}.");
        }
    }
}
