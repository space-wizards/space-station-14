using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Server.GameObjects.Components.Stack;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Utility;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using System.Collections.Generic;
using System.Linq;

namespace Content.Server.GameObjects.Components.Power
{
    [RegisterComponent]
    internal class WirePlacerComponent : Component, IAfterInteract
    {
#pragma warning disable 649
        [Dependency] private readonly IServerEntityManager _entityManager;
        [Dependency] private readonly IMapManager _mapManager;
#pragma warning restore 649

        /// <inheritdoc />
        public override string Name => "WirePlacer";

        private string _wirePrototypeID;

        /// <summary>
        ///     When placing a wire, if there is a <see cref="NodeContainerComponent"/> whose
        ///     <see cref="Node"/>s have the same set of <see cref="Node.NodeGroupID"/>s
        ///     as this, a wire will not be placed. Should generally be the same set of
        ///     <see cref="NodeGroupID"/>s as the type of wire to be placed.
        /// </summary>
        private List<NodeGroupID> _blockingNodeGroupIDs;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _wirePrototypeID, "wirePrototypeID", "HVWire");
            serializer.DataField(ref _blockingNodeGroupIDs, "blockingNodeGroupIDs", new List<NodeGroupID> { NodeGroupID.HVPower });
        }

        /// <inheritdoc />
        public void AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (!InteractionChecks.InRangeUnobstructed(eventArgs)) return;
            if(!_mapManager.TryGetGrid(eventArgs.ClickLocation.GridID, out var grid))
                return;
            var snapPos = grid.SnapGridCellFor(eventArgs.ClickLocation, SnapGridOffset.Center);
            var snapCell = grid.GetSnapGridCell(snapPos, SnapGridOffset.Center);
            if(grid.GetTileRef(snapPos).Tile.IsEmpty)
                return;
            foreach (var snapComp in snapCell)
            {
                if (snapComp.Owner.TryGetComponent<NodeContainerComponent>(out var nodeContainer))
                {
                    var nodeGroupIDs = nodeContainer.Nodes.Select(node => node.NodeGroupID).ToList();
                    if (_blockingNodeGroupIDs.All(nodeGroupIDs.Contains))
                    {
                        return;
                    }
                }
            }
            if (Owner.TryGetComponent(out StackComponent stack) && !stack.Use(1))
                return;
            _entityManager.SpawnEntity(_wirePrototypeID, grid.GridTileToLocal(snapPos));
        }
    }
}
