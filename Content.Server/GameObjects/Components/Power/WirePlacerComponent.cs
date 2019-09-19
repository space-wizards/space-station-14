using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Maps;
using Robust.Server.GameObjects;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.GameObjects.Components.Power
{
    [RegisterComponent]
    internal class WirePlacerComponent : Component, IAfterAttack
    {
#pragma warning disable 169
        [Dependency] private readonly IServerEntityManager _entityManager;
        [Dependency] private readonly IMapManager _mapManager;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager;
#pragma warning restore 169

        /// <inheritdoc />
        public override string Name => "WirePlacer";

        /// <inheritdoc />
        public void AfterAttack(AfterAttackEventArgs eventArgs)
        {
            if(!_mapManager.TryGetGrid(eventArgs.ClickLocation.GridID, out var grid))
                return;

            var snapPos = grid.SnapGridCellFor(eventArgs.ClickLocation.ToWorld(_mapManager), SnapGridOffset.Center);
            var snapCell = grid.GetSnapGridCell(snapPos, SnapGridOffset.Center);

            if(grid.GetTileRef(snapPos).Tile.IsEmpty)
                return;

            var found = false;
            foreach (var snapComp in snapCell)
            {
                if (!snapComp.Owner.HasComponent<PowerTransferComponent>())
                    continue;

                found = true;
                break;
            }

            if (found)
                return;
            
            var newWire = _entityManager.SpawnEntityAt("Wire", grid.GridTileToLocal(snapPos));
            if (newWire.TryGetComponent(out SpriteComponent wireSpriteComp)
                && Owner.TryGetComponent(out SpriteComponent itemSpriteComp))
            {
                wireSpriteComp.Color = itemSpriteComp.Color;
            }

            //TODO: There is no way to set this wire as above or below the floor
        }
    }
}
