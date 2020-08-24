using Content.Server.GameObjects.Components.Stack;
using Content.Server.Utility;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Power
{
    [RegisterComponent]
    internal class WirePlacerComponent : Component, IAfterInteract
    {
        [Dependency] private readonly IServerEntityManager _entityManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;

        /// <inheritdoc />
        public override string Name => "WirePlacer";

        [ViewVariables]
        private string _wirePrototypeID;

        [ViewVariables]
        private WireType _blockingWireType;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _wirePrototypeID, "wirePrototypeID", "HVWire");
            serializer.DataField(ref _blockingWireType, "blockingWireType", WireType.HighVoltage);
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
                if (snapComp.Owner.TryGetComponent<WireComponent>(out var wire) && wire.WireType == _blockingWireType)
                {
                    return;
                }
            }
            if (Owner.TryGetComponent(out StackComponent stack) && !stack.Use(1))
                return;
            _entityManager.SpawnEntity(_wirePrototypeID, grid.GridTileToLocal(snapPos));
        }
    }
}
