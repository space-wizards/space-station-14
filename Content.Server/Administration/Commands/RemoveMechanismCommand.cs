using Content.Server.Body;
using Content.Server.Body.Mechanism;
using Content.Server.Body.Part;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Fun)]
    public class RemoveMechanismCommand : IConsoleCommand
    {
        public string Command => "rmmechanism";
        public string Description => "Removes a given entity from it's containing bodypart, if any.";
        public string Help => "Usage: rmmechanism <uid>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
                return;
            }

            if (!EntityUid.TryParse(args[0], out var entityUid))
            {
                shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
                return;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();

            if (!entityManager.TryGetComponent<ITransformComponent>(entityUid, out var transform)) return;

            var parent = transform.ParentUid;

            if (entityManager.TryGetComponent<BodyPartComponent>(parent, out var body) &&
                entityManager.TryGetComponent<MechanismComponent>(entityUid, out var part))
            {
                body.RemoveMechanism(part);
            }
            else
            {
                shell.WriteError("Was not a mechanism, or did not have a parent.");
            }
        }
    }
}
