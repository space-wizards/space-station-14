using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class AddEntityStorageCommand : LocalizedEntityCommands
    {
        [Dependency] private readonly EntityStorageSystem _storageSystem = default!;

        public override string Command => "addstorage";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 2)
            {
                shell.WriteLine(Loc.GetString("shell-wrong-arguments-number-need-specific",
                    ("$properAmount", 2),
                    ("currentAmount", args.Length)));
                return;
            }

            if (!NetEntity.TryParse(args[0], out var entityUidNet) || !EntityManager.TryGetEntity(entityUidNet, out var entityUid))
            {
                shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
                return;
            }

            if (!NetEntity.TryParse(args[1], out var storageUidNet) || !EntityManager.TryGetEntity(storageUidNet, out var storageUid))
            {
                shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
                return;
            }

            if (EntityManager.HasComponent<EntityStorageComponent>(storageUid))
                _storageSystem.Insert(entityUid.Value, storageUid.Value);
            else
                shell.WriteError(Loc.GetString($"cmd-addstorage-failure"));
        }
    }
}
