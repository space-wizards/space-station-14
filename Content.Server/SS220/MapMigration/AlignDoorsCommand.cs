using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Doors.Components;
using Robust.Shared.Console;
using Robust.Shared.Map.Components;

namespace Content.Server.SS220.MapMigration
{
    [AdminCommand(AdminFlags.Mapping)]
    public sealed class AlignAirlocksCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entManager = default!;

        // ReSharper disable once StringLiteralTypo
        public string Command => "aligndoors";
        public string Description => "Aligns all doors with walls or neighburing doors";
        public string Help => $"Usage: {Command} <gridId> | {Command}";

        public void Execute(IConsoleShell shell, string argsOther, string[] args)
        {
            var player = shell.Player;
            EntityUid? gridId;
            var xformQuery = _entManager.GetEntityQuery<TransformComponent>();

            var mapMigration = _entManager.EntitySysManager.GetEntitySystem<MapMigrationSystem_SS220>();

            switch (args.Length)
            {
                case 0:
                    if (player?.AttachedEntity is not { Valid: true } playerEntity)
                    {
                        shell.WriteError("Only a player can run this command.");
                        return;
                    }

                    gridId = xformQuery.GetComponent(playerEntity).GridUid;
                    break;
                case 1:
                    if (!NetEntity.TryParse(args[0], out var idNet) || !_entManager.TryGetEntity(idNet, out var id))
                    {
                        shell.WriteError($"{args[0]} is not a valid entity.");
                        return;
                    }

                    gridId = id;
                    break;
                default:
                    shell.WriteLine(Help);
                    return;
            }

            if (!_entManager.TryGetComponent(gridId, out MapGridComponent? grid))
            {
                shell.WriteError($"No grid exists with id {gridId}");
                return;
            }

            if (!_entManager.EntityExists(gridId))
            {
                shell.WriteError($"Grid {gridId} doesn't have an associated grid entity.");
                return;
            }

            var processed = 0;

            foreach (var child in xformQuery.GetComponent(gridId.Value).ChildEntities)
            {
                if (!_entManager.EntityExists(child))
                {
                    continue;
                }

                var valid = _entManager.HasComponent<DoorComponent>(child);

                if (!valid)
                    continue;

                var childXform = xformQuery.GetComponent(child);

                mapMigration.RotateDoor(child, gridId);
                processed++;
            }

            shell.WriteLine($"Processed {processed} entities. If things seem wrong, reconnect.");
        }
    }
}
