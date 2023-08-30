using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class RemoveEntityStorageCommand : IConsoleCommand
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

            if (!entityManager.EntitySysManager.TryGetEntitySystem<EntityStorageSystem>(out var entstorage)) return;
            if (!entityManager.TryGetComponent<TransformComponent>(entityUid, out var transform)) return;

            var parent = transform.ParentUid;

            if (entityManager.TryGetComponent<EntityStorageComponent>(parent, out var storage))
            {
                entstorage.Remove(entityUid, storage.Owner, storage);
            }
            else
            {
                shell.WriteError("Could not remove from storage.");
            }
        }
    }
}
