using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class AddEntityStorageCommand : IConsoleCommand
    {
        public string Command => "addstorage";
        public string Description => "Adds a given entity to a containing storage.";
        public string Help => "Usage: addstorage <entity uid> <storage uid>";

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

            if (entityManager.HasComponent<EntityStorageComponent>(storageUid) &&
                entityManager.EntitySysManager.TryGetEntitySystem<EntityStorageSystem>(out var storageSys))
            {
                storageSys.Insert(entityUid, storageUid);
            }
            else
            {
                shell.WriteError("Could not insert into non-storage.");
            }
        }
    }
}
