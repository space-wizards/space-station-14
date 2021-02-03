#nullable enable
using Content.Server.GameObjects.Components.Stack;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Utility;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System.Threading.Tasks;

namespace Content.Server.GameObjects.Components.Power
{
    [RegisterComponent]
    internal class WirePlacerComponent : Component, IAfterInteract
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

        /// <inheritdoc />
        public override string Name => "WirePlacer";

        [ViewVariables]
        private string? _wirePrototypeID;

        [ViewVariables]
        private WireType _blockingWireType;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _wirePrototypeID, "wirePrototypeID", "HVWire");
            serializer.DataField(ref _blockingWireType, "blockingWireType", WireType.HighVoltage);
        }

        /// <inheritdoc />
        public async Task<bool> AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (_wirePrototypeID == null)
                return true;
            if (!eventArgs.InRangeUnobstructed(ignoreInsideBlocker: true, popup: true))
                return true;
            if(!_mapManager.TryGetGrid(eventArgs.ClickLocation.GetGridId(Owner.EntityManager), out var grid))
                return true;
            var snapPos = grid.SnapGridCellFor(eventArgs.ClickLocation, SnapGridOffset.Center);
            var snapCell = grid.GetSnapGridCell(snapPos, SnapGridOffset.Center);
            if(grid.GetTileRef(snapPos).Tile.IsEmpty)
                return true;
            foreach (var snapComp in snapCell)
            {
                if (snapComp.Owner.TryGetComponent<WireComponent>(out var wire) && wire.WireType == _blockingWireType)
                {
                    return true;
                }
            }
            if (Owner.TryGetComponent<StackComponent>(out var stack) && !stack.Use(1))
                return true;
            Owner.EntityManager.SpawnEntity(_wirePrototypeID, grid.GridTileToLocal(snapPos));
            return true;
        }
    }
}
