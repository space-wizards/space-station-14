using Content.Server.Body.Components;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Fun)]
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

            if (!EntityUid.TryParse(args[0], out var entityUid))
            {
                shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
                return;
            }

            if (!EntityUid.TryParse(args[1], out var storageUid))
            {
                shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
                return;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();

            if (entityManager.TryGetComponent<BodyComponent>(storageUid, out var storage)
                && entityManager.TryGetComponent<BodyPartComponent>(entityUid, out var bodyPart))
            {
                if (storage.TryAddPart(args[3], bodyPart))
                {
                    shell.WriteLine($@"Added {entityUid} to {storageUid}.");
                }
                else
                {
                    shell.WriteError($@"Could not add {entityUid} to {storageUid}.");
                }
            }
            else
            {
                shell.WriteError("Could not insert.");
            }
        }
    }
}
