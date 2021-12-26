using System.Collections.Generic;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.Power.Nodes;
using Content.Shared.Wires;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.Power.EntitySystems
{
    [UsedImplicitly]
    public sealed class CableVisSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

        private readonly HashSet<EntityUid> _toUpdate = new();

        public void QueueUpdate(EntityUid uid)
        {
            _toUpdate.Add(uid);
        }

        public override void Initialize()
        {
            base.Initialize();

            UpdatesAfter.Add(typeof(NodeGroupSystem));
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var uid in _toUpdate)
            {
                if (!EntityManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer)
                    || !EntityManager.TryGetComponent(uid, out CableVisComponent? cableVis)
                    || !EntityManager.TryGetComponent(uid, out AppearanceComponent? appearance))
                {
                    continue;
                }

                if (cableVis.Node == null)
                    continue;

                var mask = WireVisDirFlags.None;

                var transform = EntityManager.GetComponent<TransformComponent>(uid);

                // Only valid grids allowed.
                if(!transform.GridID.IsValid())
                    continue;

                var grid = _mapManager.GetGrid(transform.GridID);
                var tile = grid.TileIndicesFor(transform.Coordinates);
                var node = nodeContainer.GetNode<CableNode>(cableVis.Node);

                foreach (var reachable in node.ReachableNodes)
                {
                    if (reachable is not CableNode)
                        continue;

                    var otherTransform = EntityManager.GetComponent<TransformComponent>(reachable.Owner);
                    if (otherTransform.GridID != grid.Index)
                        continue;

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

                appearance.SetData(WireVisVisuals.ConnectedMask, mask);
            }

            _toUpdate.Clear();
        }
    }
}
