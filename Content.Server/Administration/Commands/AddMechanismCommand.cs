using Content.Server.Body;
using Content.Server.Body.Mechanism;
using Content.Server.Body.Part;
using Content.Server.Storage.Components;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Fun)]
    public class AddMechanismCommand : IConsoleCommand
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

            if (entityManager.TryGetComponent<BodyPartComponent>(storageUid, out var storage)
                && entityManager.TryGetComponent<MechanismComponent>(entityUid, out var bodyPart))
            {
                if (storage.TryAddMechanism(bodyPart))
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
