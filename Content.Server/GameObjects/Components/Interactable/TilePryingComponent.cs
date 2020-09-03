using Content.Server.GameObjects.EntitySystems.Click;
using Content.Shared.GameObjects.Components.Interactable;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Maps;
using Content.Shared.Utility;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Interactable
{
    [RegisterComponent]
    public class TilePryingComponent : Component, IAfterInteract
    {
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;

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

        public async void TryPryTile(IEntity user, GridCoordinates clickLocation)
        {
            if (!Owner.TryGetComponent<ToolComponent>(out var tool) && _toolComponentNeeded)
                return;

            var mapGrid = _mapManager.GetGrid(clickLocation.GridID);
            var tile = mapGrid.GetTileRef(clickLocation);

            var coordinates = mapGrid.GridTileToLocal(tile.GridIndices);

            if (!user.InRangeUnobstructed(coordinates, popup: true))
                return;

            var tileDef = (ContentTileDefinition)_tileDefinitionManager[tile.Tile.TypeId];

            if (!tileDef.CanCrowbar) return;

            if (_toolComponentNeeded && !await tool!.UseTool(user, null, 0f,  ToolQuality.Prying))
                return;

            coordinates.PryTile(_mapManager, _tileDefinitionManager, Owner.EntityManager);
        }
    }
}
