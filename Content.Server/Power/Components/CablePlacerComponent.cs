#nullable enable
using System.Threading.Tasks;
using Content.Server.Power.Components;
using Content.Server.Stack;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.WireHacking.Components
{
    [RegisterComponent]
    internal class CablePlacerComponent : Component, IAfterInteract
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

        /// <inheritdoc />
        public override string Name => "CablePlacer";

        [ViewVariables]
        [DataField("wirePrototypeID")]
        private string? _wirePrototypeID = "HVCable";

        [ViewVariables]
        [DataField("blockingWireType")]
        private CableType _blockingCableType = CableType.HighVoltage;

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
                if (Owner.EntityManager.ComponentManager.TryGetComponent<CableComponent>(anchored, out var wire) && wire.CableType == _blockingCableType)
                {
                    return true;
                }
            }

            if (Owner.TryGetComponent<StackComponent>(out var stack)
                && !EntitySystem.Get<StackSystem>().Use(Owner.Uid, stack, 1))
                return true;

            Owner.EntityManager.SpawnEntity(_wirePrototypeID, grid.GridTileToLocal(snapPos));
            return true;
        }
    }
}
