using Content.Server.Administration;
using Content.Server.Power.Components;
using Content.Shared.Administration;
using Content.Shared.Construction;
using Content.Shared.Tag;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Map;

namespace Content.Server.Construction.Commands
{
    [AdminCommand(AdminFlags.Mapping)]
    sealed class FixRotationsCommand : IConsoleCommand
    {
        // ReSharper disable once StringLiteralTypo
        public string Command => "fixrotations";
        public string Description => "Sets the rotation of all occluders, low walls and windows to south.";
        public string Help => $"Usage: {Command} <gridId> | {Command}";

        public void Execute(IConsoleShell shell, string argsOther, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            var entityManager = IoCManager.Resolve<IEntityManager>();
            EntityUid? gridId;
            var xformQuery = entityManager.GetEntityQuery<TransformComponent>();

            switch (args.Length)
            {
                case 0:
                    if (player?.AttachedEntity is not {Valid: true} playerEntity)
                    {
                        shell.WriteError("Only a player can run this command.");
                        return;
                    }

                    gridId = xformQuery.GetComponent(playerEntity).GridUid;
                    break;
                case 1:
                    if (!EntityUid.TryParse(args[0], out var id))
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

            var mapManager = IoCManager.Resolve<IMapManager>();
            if (!mapManager.TryGetGrid(gridId, out var grid))
            {
                shell.WriteError($"No grid exists with id {gridId}");
                return;
            }

            if (!entityManager.EntityExists(grid.GridEntityId))
            {
                shell.WriteError($"Grid {gridId} doesn't have an associated grid entity.");
                return;
            }

            var changed = 0;
            var tagSystem = entityManager.EntitySysManager.GetEntitySystem<TagSystem>();

            foreach (var child in xformQuery.GetComponent(grid.GridEntityId).ChildEntities)
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
                valid |= tagSystem.HasTag(child, "ForceFixRotations");
                // override
                valid &= !tagSystem.HasTag(child, "ForceNoFixRotations");

                if (!valid)
                    continue;

                var childXform = xformQuery.GetComponent(child);

                if (childXform.LocalRotation != Angle.Zero)
                {
                    childXform.LocalRotation = Angle.Zero;
                    changed++;
                }
            }

            shell.WriteLine($"Changed {changed} entities. If things seem wrong, reconnect.");
        }
    }
}
