using Content.Server.Body.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class AddMechanismCommand : IConsoleCommand
    {
        public string Command => "addmechanism";
        public string Description => "Adds a given entity to a containing body.";
        public string Help => "Usage: addmechanism <entity uid> <bodypart uid>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 2)
            {
                shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
                return;
            }

            if (!EntityUid.TryParse(args[0], out var organId))
            {
                shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
                return;
            }

            if (!EntityUid.TryParse(args[1], out var partId))
            {
                shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
                return;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();
            var bodySystem = entityManager.System<BodySystem>();

            if (bodySystem.AddOrganToFirstValidSlot(organId, partId))
            {
                shell.WriteLine($@"Added {organId} to {partId}.");
            }
            else
            {
                shell.WriteError($@"Could not add {organId} to {partId}.");
            }
        }
    }
}
