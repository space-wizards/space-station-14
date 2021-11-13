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
    public class RemoveEntityStorageCommand : IConsoleCommand
    {
        public string Command => "rmstorage";
        public string Description => "Removes a given entity from it's containing storage, if any.";
        public string Help => "Usage: rmstorage <uid>";

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

            if (!entityManager.TryGetComponent<TransformComponent>(entityUid, out var transform)) return;

            var parent = transform.ParentUid;

            if (entityManager.TryGetComponent<EntityStorageComponent>(parent, out var storage))
            {
                storage.Remove(entityManager.GetEntity(entityUid));
            }
            else
            {
                shell.WriteError("Could not remove from storage.");
            }
        }
    }
}
