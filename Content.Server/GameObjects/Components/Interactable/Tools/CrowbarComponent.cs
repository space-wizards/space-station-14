using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Maps;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.GameObjects.Components.Interactable.Tools
{
    [RegisterComponent]
    public class CrowbarComponent : ToolComponent, IAfterAttack
    {
#pragma warning disable 649
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager;
        [Dependency] private readonly IMapManager _mapManager;
#pragma warning restore 649

        /// <summary>
        /// Tool that can be used to crowbar things apart, such as deconstructing
        /// </summary>
        public override string Name => "Crowbar";

        public void AfterAttack(AfterAttackEventArgs eventArgs)
        {
            var mapGrid = _mapManager.GetGrid(eventArgs.ClickLocation.GridID);
            var tile = mapGrid.GetTileRef(eventArgs.ClickLocation);

            var coordinates = mapGrid.GridTileToLocal(tile.GridIndices);
            float distance = coordinates.Distance(_mapManager, Owner.Transform.GridPosition);

            if (distance > InteractionSystem.InteractionRange)
            {
                return;
            }

            var tileDef = (ContentTileDefinition)_tileDefinitionManager[tile.Tile.TypeId];
            if (tileDef.CanCrowbar)
            {
                var underplating = _tileDefinitionManager["underplating"];
                mapGrid.SetTile(eventArgs.ClickLocation, new Tile(underplating.TileId));
               _entitySystemManager.GetEntitySystem<AudioSystem>().Play("/Audio/items/crowbar.ogg", Owner);
                //Actually spawn the relevant tile item at the right position and give it some offset to the corner.
                var tileItem = Owner.EntityManager.SpawnEntity(tileDef.ItemDropPrototypeName, coordinates);
                tileItem.Transform.WorldPosition += (0.2f, 0.2f);
            }
        }
    }
}
