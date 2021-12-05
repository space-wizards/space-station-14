using Content.Server.Administration;
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
            var entityManager = IoCManager.Resolve<IEntityManager>();
            GridId gridId;

            switch (args.Length)
            {
                case 0:
                    if (player?.AttachedEntity is not {Valid: true} playerEntity)
                    {
                        shell.WriteLine("Only a player can run this command.");
                        return;
                    }

                    gridId = entityManager.GetComponent<TransformComponent>(playerEntity).GridID;
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

            if (!entityManager.EntityExists(grid.GridEntityId))
            {
                shell.WriteLine($"Grid {gridId} doesn't have an associated grid entity.");
                return;
            }

            var changed = 0;
            foreach (var child in entityManager.GetComponent<TransformComponent>(grid.GridEntityId).ChildEntities)
            {
                if (!entityManager.EntityExists(child))
                {
                    continue;
                }

                var valid = false;

                // Occluders should only count if the state of it right now is enabled.
                // This prevents issues with edge firelocks.
                if (entityManager.TryGetComponent<OccluderComponent>(child, out var occluder))
                {
                    valid |= occluder.Enabled;
                }
                // low walls & grilles
                valid |= entityManager.HasComponent<SharedCanBuildWindowOnTopComponent>(child);
                // cables
                valid |= entityManager.HasComponent<CableComponent>(child);
                // anything else that might need this forced
                valid |= child.HasTag("ForceFixRotations");
                // override
                valid &= !child.HasTag("ForceNoFixRotations");

                if (!valid)
                {
                    continue;
                }

                if (entityManager.GetComponent<TransformComponent>(child).LocalRotation != Angle.Zero)
                {
                    entityManager.GetComponent<TransformComponent>(child).LocalRotation = Angle.Zero;
                    changed++;
                }
            }

            shell.WriteLine($"Changed {changed} entities. If things seem wrong, reconnect.");
        }
    }
}
