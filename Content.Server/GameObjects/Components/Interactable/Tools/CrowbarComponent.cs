using System;
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

        private double MAX_DISTANCE = 1.5;

        /// <summary>
        /// Tool that can be used to crowbar things apart, such as deconstructing
        /// </summary>
        public override string Name => "Crowbar";

        public void AfterAttack(AfterAttackEventArgs eventArgs)
        {
            var mapGrid = _mapManager.GetGrid(eventArgs.ClickLocation.GridID);
            var tile = mapGrid.GetTileRef(eventArgs.ClickLocation);

            var ownerLocation = Owner.Transform.GridPosition;
            var tileCenterX = tile.X + 0.5;
            var tileCenterY = tile.Y + 0.5;

            if (Distance(ownerLocation.X, tileCenterX, ownerLocation.Y, tileCenterY) > MAX_DISTANCE)
            {
                return;
            }

            var tileDef = (ContentTileDefinition)_tileDefinitionManager[tile.Tile.TypeId];
            if (tileDef.CanCrowbar)
            {
                var underplating = _tileDefinitionManager["underplating"];
                mapGrid.SetTile(eventArgs.ClickLocation, new Tile(underplating.TileId));
               _entitySystemManager.GetEntitySystem<AudioSystem>().Play("/Audio/items/crowbar.ogg", Owner);
            }
        }

        private double Distance(float x1, double x2, float y1, double y2)
        {
            return Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2));
        }
    }
}
