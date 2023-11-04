using Content.Server.Body.Systems;
using Content.Shared.Administration;
using Content.Shared.Body.Part;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class AddBodyPartCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entManager = default!;

        public string Command => "addbodypart";
        public string Description => "Adds a given entity to a containing body.";
        public string Help => "Usage: addbodypart <entity uid> <body uid> <part slot> <part type>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 3)
            {
                shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
                return;
            }

            if (!NetEntity.TryParse(args[0], out var childNetId))
            {
                shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
                return;
            }

            if (!NetEntity.TryParse(args[1], out var parentNetId))
            {
                shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
                return;
            }

            var childId = _entManager.GetEntity(childNetId);
            var parentId = _entManager.GetEntity(parentNetId);
            var bodySystem = _entManager.System<BodySystem>();



            if (Enum.TryParse<BodyPartType>(args[3], out var partType) &&
                bodySystem.TryCreatePartSlotAndAttach(parentId, args[2], childId, partType))
            {
                shell.WriteLine($@"Added {childId} to {parentId}.");
            }
            else
            {
                shell.WriteError($@"Could not add {childId} to {parentId}.");
            }
        }
    }
}
