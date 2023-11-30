using Content.Server.Body.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class AddMechanismCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entManager = default!;

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

            if (!NetEntity.TryParse(args[0], out var organIdNet) || !_entManager.TryGetEntity(organIdNet, out var organId))
            {
                shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
                return;
            }

            if (!NetEntity.TryParse(args[1], out var partIdNet) || !_entManager.TryGetEntity(partIdNet, out var partId))
            {
                shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
                return;
            }

            var bodySystem = _entManager.System<BodySystem>();

            if (bodySystem.AddOrganToFirstValidSlot(partId.Value, organId.Value))
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
