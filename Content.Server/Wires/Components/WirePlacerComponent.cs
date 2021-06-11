#nullable enable
using System.Threading.Tasks;
using Content.Server.Stack;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Wires.Components
{
    [RegisterComponent]
    internal class WirePlacerComponent : Component, IAfterInteract
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

        /// <inheritdoc />
        public override string Name => "WirePlacer";

        [ViewVariables]
        [DataField("wirePrototypeID")]
        private string? _wirePrototypeID = "HVWire";

        [ViewVariables]
        [DataField("blockingWireType")]
        private WireType _blockingWireType = WireType.HighVoltage;

        /// <inheritdoc />
        async Task<bool> IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (_wirePrototypeID == null)
                return true;
            if (!eventArgs.InRangeUnobstructed(ignoreInsideBlocker: true, popup: true))
                return true;
            if(!_mapManager.TryGetGrid(eventArgs.ClickLocation.GetGridId(Owner.EntityManager), out var grid))
                return true;
            var snapPos = grid.TileIndicesFor(eventArgs.ClickLocation);
            if(grid.GetTileRef(snapPos).Tile.IsEmpty)
                return true;
            foreach (var anchored in grid.GetAnchoredEntities(snapPos))
            {
                if (Owner.EntityManager.ComponentManager.TryGetComponent<WireComponent>(anchored, out var wire) && wire.WireType == _blockingWireType)
                {
                    return true;
                }
            }

            if (Owner.HasComponent<StackComponent>())
            {
                var stackUse = new StackUseEvent(){Amount = 1};
                Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, stackUse);

                if(!stackUse.Result)
                    return true;
            }

            Owner.EntityManager.SpawnEntity(_wirePrototypeID, grid.GridTileToLocal(snapPos));
            return true;
        }
    }
}
