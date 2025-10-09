using Content.Shared.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class RemoveEntityStorageCommand : LocalizedCommands
    {
        [Dependency] private readonly IEntityManager _entManager = default!;

        public override string Command => "rmstorage";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
                return;
            }

            if (!NetEntity.TryParse(args[0], out var entityNet) || !_entManager.TryGetEntity(entityNet, out var entityUid))
            {
                shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
                return;
            }

            if (!_entManager.EntitySysManager.TryGetEntitySystem<EntityStorageSystem>(out var entstorage))
                return;

            if (!_entManager.TryGetComponent<TransformComponent>(entityUid, out var transform))
                return;

            var parent = transform.ParentUid;

            if (_entManager.TryGetComponent<EntityStorageComponent>(parent, out var storage))
            {
                entstorage.Remove(entityUid.Value, parent, storage);
            }
            else
            {
                shell.WriteError(Loc.GetString("cmd-rmstorage-error-remove"));
            }
        }
    }
}
