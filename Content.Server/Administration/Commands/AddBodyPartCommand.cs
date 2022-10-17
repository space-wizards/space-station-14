using Content.Server.Body.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class AddBodyPartCommand : IConsoleCommand
    {
        public string Command => "addbodypart";
        public string Description => "Adds a given entity to a containing body.";
        public string Help => "Usage: addbodypart <entity uid> <body uid> <part slot>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 3)
            {
                shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
                return;
            }

            if (!EntityUid.TryParse(args[0], out var childId))
            {
                shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
                return;
            }

            if (!EntityUid.TryParse(args[1], out var parentId))
            {
                shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
                return;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();
            var bodySystem = entityManager.System<BodySystem>();

            if (bodySystem.TryCreatePartSlotAndAttach(parentId, args[3], childId))
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
