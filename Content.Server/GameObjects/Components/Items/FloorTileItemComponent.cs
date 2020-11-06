using Content.Server.GameObjects.Components.Stack;
using Content.Shared.Audio;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Maps;
using Content.Shared.Utility;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Items
{
    [RegisterComponent]
    public class FloorTileItemComponent : Component, IAfterInteract
    {
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;

        public override string Name => "FloorTile";
        private string _outputTile;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _outputTile, "output", "floor_steel");
        }

        public override void Initialize()
        {
            base.Initialize();
            Owner.EnsureComponent<StackComponent>();
        }

        private bool HasBaseTurf(ContentTileDefinition tileDef, string baseTurf)
        {
            foreach (var tileBaseTurf in tileDef.BaseTurfs)
            {
                if (baseTurf == tileBaseTurf)
                {
                    return true;
                }
            }

            return false;
        }

        private void PlaceAt(IMapGrid mapGrid, EntityCoordinates location, ushort tileId, float offset = 0)
        {
            mapGrid.SetTile(location.Offset(new Vector2(offset, offset)), new Tile(tileId));
            EntitySystem.Get<AudioSystem>().PlayAtCoords("/Audio/Items/genhit.ogg", location, AudioHelpers.WithVariation(0.125f));
        }

        public void AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (!eventArgs.InRangeUnobstructed(ignoreInsideBlocker: true, popup: true)) return;
            if (!Owner.TryGetComponent(out StackComponent stack)) return;

            var location = eventArgs.ClickLocation.AlignWithClosestGridTile();
            var locationMap = location.ToMap(Owner.EntityManager);

            var desiredTile = (ContentTileDefinition)_tileDefinitionManager[_outputTile];

            if (_mapManager.TryGetGrid(location.GetGridId(Owner.EntityManager), out var mapGrid))
            {
                var tile = mapGrid.GetTileRef(location);
                var baseTurf = (ContentTileDefinition)_tileDefinitionManager[tile.Tile.TypeId];

                if (HasBaseTurf(desiredTile, baseTurf.Name) && eventArgs.Target == null && stack.Use(1))
                {
                    PlaceAt(mapGrid, location, desiredTile.TileId);
                }
            }
            else if(HasBaseTurf(desiredTile, "space"))
            {
                mapGrid = _mapManager.CreateGrid(locationMap.MapId);
                mapGrid.WorldPosition = locationMap.Position;
                location = new EntityCoordinates(mapGrid.GridEntityId, Vector2.Zero);
                PlaceAt(mapGrid, location, desiredTile.TileId, mapGrid.TileSize/2f);
            }

        }

    }
}
