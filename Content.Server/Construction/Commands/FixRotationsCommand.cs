using Content.Server.Administration;
using Content.Server.Window;
using Content.Server.Power.Components;
using Content.Shared.Administration;
using Content.Shared.Construction;
using Content.Shared.Tag;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Server.Construction.Commands
{
    [AdminCommand(AdminFlags.Mapping)]
    class FixRotationsCommand : IConsoleCommand
    {
        // ReSharper disable once StringLiteralTypo
        public string Command => "fixrotations";
        public string Description => "Sets the rotation of all occluders, low walls and windows to south.";
        public string Help => $"Usage: {Command} <gridId> | {Command}";

        public void Execute(IConsoleShell shell, string argsOther, string[] args)
        {
            var player = shell.Player as IPlayerSession;

            GridId gridId;

            switch (args.Length)
            {
                case 0:
                    if (player?.AttachedEntity == null)
                    {
                        shell.WriteLine("Only a player can run this command.");
                        return;
                    }

                    gridId = player.AttachedEntity.Transform.GridID;
                    break;
                case 1:
                    if (!int.TryParse(args[0], out var id))
                    {
                        shell.WriteLine($"{args[0]} is not a valid integer.");
                        return;
                    }

                    gridId = new GridId(id);
                    break;
                default:
                    shell.WriteLine(Help);
                    return;
            }

            var mapManager = IoCManager.Resolve<IMapManager>();
            if (!mapManager.TryGetGrid(gridId, out var grid))
            {
                shell.WriteLine($"No grid exists with id {gridId}");
                return;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();
            if (!entityManager.TryGetEntity(grid.GridEntityId, out var gridEntity))
            {
                shell.WriteLine($"Grid {gridId} doesn't have an associated grid entity.");
                return;
            }

            var changed = 0;
            foreach (var childUid in gridEntity.Transform.ChildEntityUids)
            {
                if (!entityManager.TryGetEntity(childUid, out var childEntity))
                {
                    continue;
                }

                var valid = false;

                // Occluders should only count if the state of it right now is enabled.
                // This prevents issues with edge firelocks.
                if (entityManager.TryGetComponent<OccluderComponent>(childUid, out var occluder))
                {
                    valid |= occluder.Enabled;
                }
                // low walls & grilles
                valid |= childEntity.HasComponent<SharedCanBuildWindowOnTopComponent>();
                // cables
                valid |= childEntity.HasComponent<CableComponent>();
                // anything else that might need this forced
                valid |= childEntity.HasTag("ForceFixRotations");
                // override
                valid &= !childEntity.HasTag("ForceNoFixRotations");

                if (!valid)
                {
                    continue;
                }

                if (childEntity.Transform.LocalRotation != Angle.Zero)
                {
                    childEntity.Transform.LocalRotation = Angle.Zero;
                    changed++;
                }
            }

            shell.WriteLine($"Changed {changed} entities. If things seem wrong, reconnect.");
        }
    }
}
