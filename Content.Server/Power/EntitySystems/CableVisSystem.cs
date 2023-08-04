using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.Power.Nodes;
using Content.Shared.Wires;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Map;

namespace Content.Server.Power.EntitySystems
{
    [UsedImplicitly]
    public sealed class CableVisSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<CableVisComponent, NodeGroupsRebuilt>(UpdateAppearance);
        }

        private void UpdateAppearance(EntityUid uid, CableVisComponent cableVis, ref NodeGroupsRebuilt args)
        {
            if (cableVis.Node == null)
                return;

            if (!TryComp(uid, out NodeContainerComponent? nodeContainer) || !TryComp(uid, out AppearanceComponent? appearance))
                return;

            var transform = Transform(uid);
            if (!_mapManager.TryGetGrid(transform.GridUid, out var grid))
                return;

            var mask = WireVisDirFlags.None;
            var tile = grid.TileIndicesFor(transform.Coordinates);
            var node = nodeContainer.GetNode<CableNode>(cableVis.Node);

            foreach (var reachable in node.ReachableNodes)
            {
                if (reachable is not CableNode)
                    continue;

                var otherTransform = Transform(reachable.Owner);
                var otherTile = grid.TileIndicesFor(otherTransform.Coordinates);
                var diff = otherTile - tile;

                mask |= diff switch
                {
                    (0, 1) => WireVisDirFlags.North,
                    (0, -1) => WireVisDirFlags.South,
                    (1, 0) => WireVisDirFlags.East,
                    (-1, 0) => WireVisDirFlags.West,
                    _ => WireVisDirFlags.None
                };
            }

            _appearance.SetData(uid, WireVisVisuals.ConnectedMask, mask, appearance);
        }
    }
}
