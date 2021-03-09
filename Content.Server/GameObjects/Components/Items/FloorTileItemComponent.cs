using Content.Server.GameObjects.Components.Stack;
using Content.Shared.Audio;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Maps;
using Content.Shared.Utility;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using System.Collections.Generic;
using System.Threading.Tasks;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components.Items
{
    [RegisterComponent]
    public class FloorTileItemComponent : Component, IAfterInteract
    {
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;

        public override string Name => "FloorTile";
        [DataField("outputs")]
        private List<string> _outputTiles;

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

        async Task<bool> IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (!eventArgs.InRangeUnobstructed(ignoreInsideBlocker: true, popup: true))
                return true;

            if (!Owner.TryGetComponent(out StackComponent stack))
                return true;

            var mapManager = IoCManager.Resolve<IMapManager>();

            var location = eventArgs.ClickLocation.AlignWithClosestGridTile();
            var locationMap = location.ToMap(Owner.EntityManager);
            if (locationMap.MapId == MapId.Nullspace)
                return true;
            mapManager.TryGetGrid(location.GetGridId(Owner.EntityManager), out var mapGrid);
            foreach (var currentTile in _outputTiles)
            {
                var currentTileDefinition = (ContentTileDefinition) _tileDefinitionManager[currentTile];

                if (mapGrid != null)
                {
                    var tile = mapGrid.GetTileRef(location);
                    var baseTurf = (ContentTileDefinition) _tileDefinitionManager[tile.Tile.TypeId];

                    if (HasBaseTurf(currentTileDefinition, baseTurf.Name) && stack.Use(1))
                    {
                        PlaceAt(mapGrid, location, currentTileDefinition.TileId);
                        break;
                    }
                }
                else if (HasBaseTurf(currentTileDefinition, "space"))
                {
                    mapGrid = mapManager.CreateGrid(locationMap.MapId);
                    mapGrid.WorldPosition = locationMap.Position;
                    location = new EntityCoordinates(mapGrid.GridEntityId, Vector2.Zero);
                    PlaceAt(mapGrid, location, _tileDefinitionManager[_outputTiles[0]].TileId, mapGrid.TileSize / 2f);
                    break;
                }
            }

            return true;
        }
    }
}
