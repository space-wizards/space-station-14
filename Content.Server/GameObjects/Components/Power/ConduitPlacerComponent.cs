using Content.Server.GameObjects.Components.NodeContainer;
using Content.Server.GameObjects.Components.NodeContainer.Nodes;
using Content.Server.GameObjects.Components.Stack;
using Content.Server.Interfaces.GameObjects.Components.Interaction;
using Content.Server.Utility;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System.Linq;

namespace Content.Server.GameObjects.Components.Power
{
    [RegisterComponent]
    public class ConduitPlacerComponent : Component, IAfterInteract
    {
#pragma warning disable 649
        [Dependency] private readonly IServerEntityManager _entityManager;
        [Dependency] private readonly IMapManager _mapManager;
#pragma warning restore 649

        /// <inheritdoc />
        public override string Name => "ConduitPlacer";

        [ViewVariables]
        private string _conduitPrototypeID;

        [ViewVariables(VVAccess.ReadWrite)]
        private ConduitLayer _blockingConduitLayer;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _conduitPrototypeID, "conduitPrototypeID", "HVWire");
            serializer.DataField(ref _blockingConduitLayer, "blockingConduitLayer", ConduitLayer.First);
        }

        /// <inheritdoc />
        public void AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (!InteractionChecks.InRangeUnobstructed(eventArgs))
            {
                return;
            }
            if(!_mapManager.TryGetGrid(eventArgs.ClickLocation.GridID, out var grid))
            {
                return;
            }
            var snapPos = grid.SnapGridCellFor(eventArgs.ClickLocation, SnapGridOffset.Center);
            var snapCell = grid.GetSnapGridCell(snapPos, SnapGridOffset.Center);
            if (grid.GetTileRef(snapPos).Tile.IsEmpty)
            {
                return;
            }
            foreach (var snapComp in snapCell)
            {
                if (snapComp.Owner.TryGetComponent<NodeContainerComponent>(out var container) &&
                    container.Nodes.OfType<ConduitNode>().Select(conduitNode => conduitNode.ConduitLayer).Contains(_blockingConduitLayer))
                {
                    return;
                }
            }
            if (Owner.TryGetComponent(out StackComponent stack) && !stack.Use(1))
                return;
            var conduitEntity = _entityManager.SpawnEntity(_conduitPrototypeID, grid.GridTileToLocal(snapPos));
            var conduitNodes = conduitEntity.GetComponent<NodeContainerComponent>().Nodes.OfType<ConduitNode>();
            foreach (var conduitNode in conduitNodes)
            {
                conduitNode.ConduitLayer = _blockingConduitLayer;
            }
        }
    }
}
