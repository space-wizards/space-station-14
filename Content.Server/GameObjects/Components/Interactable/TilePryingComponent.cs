using Content.Server.GameObjects.EntitySystems.Click;
using Content.Server.Interfaces.GameObjects.Components.Interaction;
using Content.Shared.GameObjects.Components.Interactable;
using Content.Shared.Maps;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Interactable
{
    [RegisterComponent]
    public class TilePryingComponent : Component, IAfterInteract
    {
#pragma warning disable 649
        [Dependency] private readonly IEntitySystemManager _entitySystemManager;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager;
        [Dependency] private readonly IMapManager _mapManager;
#pragma warning restore 649

        public override string Name => "TilePrying";
        private bool _toolComponentNeeded = true;

        public void AfterInteract(AfterInteractEventArgs eventArgs)
        {
            TryPryTile(eventArgs.User, eventArgs.ClickLocation);
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _toolComponentNeeded, "toolComponentNeeded", true);
        }

        public void TryPryTile(IEntity user, GridCoordinates clickLocation)
        {
            if (!Owner.TryGetComponent<ToolComponent>(out var tool) && _toolComponentNeeded)
                return;

            var mapGrid = _mapManager.GetGrid(clickLocation.GridID);
            var tile = mapGrid.GetTileRef(clickLocation);

            var coordinates = mapGrid.GridTileToLocal(tile.GridIndices);

            if (!_entitySystemManager.GetEntitySystem<InteractionSystem>().InRangeUnobstructed(user.Transform.MapPosition, coordinates.ToMap(_mapManager), ignoredEnt:user))
                return;

            var tileDef = (ContentTileDefinition)_tileDefinitionManager[tile.Tile.TypeId];

            if (!tileDef.CanCrowbar) return;

            if (_toolComponentNeeded && !tool.UseTool(user, null, ToolQuality.Prying))
                return;

            var underplating = _tileDefinitionManager["underplating"];
            mapGrid.SetTile(clickLocation, new Tile(underplating.TileId));

            //Actually spawn the relevant tile item at the right position and give it some offset to the corner.
            var tileItem = Owner.EntityManager.SpawnEntity(tileDef.ItemDropPrototypeName, coordinates);
            tileItem.Transform.WorldPosition += (0.2f, 0.2f);
        }
    }
}
