using Content.Server.Stack;
using Content.Shared.Audio;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Sound;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Tiles
{
    public sealed class FloorTileSystem : EntitySystem
    {
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly StackSystem _stackSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<FloorTileComponent, AfterInteractEvent>(OnAfterInteract);
        }

        private void OnAfterInteract(EntityUid uid, FloorTileComponent component, AfterInteractEvent args)
        {
            if (!args.CanReach)
                return;

            if (!TryComp<StackComponent>(uid, out var stack))
                return;

            if (component.OutputTiles == null)
                return;

            // this looks a bit sussy but it might be because it needs to be able to place off of grids and expand them
            var location = args.ClickLocation.AlignWithClosestGridTile();
            var locationMap = location.ToMap(EntityManager);
            if (locationMap.MapId == MapId.Nullspace)
                return;
            _mapManager.TryGetGrid(location.GetGridId(EntityManager), out var mapGrid);

            foreach (var currentTile in component.OutputTiles)
            {
                var currentTileDefinition = (ContentTileDefinition) _tileDefinitionManager[currentTile];

                if (mapGrid != null)
                {
                    var tile = mapGrid.GetTileRef(location);
                    var baseTurf = (ContentTileDefinition) _tileDefinitionManager[tile.Tile.TypeId];

                    if (HasBaseTurf(currentTileDefinition, baseTurf.ID))
                    {
                        if (!_stackSystem.Use(uid, 1, stack))
                            continue;

                        PlaceAt(mapGrid, location, currentTileDefinition.TileId, component.PlaceTileSound);
                    }
                }
                else if (HasBaseTurf(currentTileDefinition, "space"))
                {
                    mapGrid = _mapManager.CreateGrid(locationMap.MapId);
                    mapGrid.WorldPosition = locationMap.Position;
                    location = new EntityCoordinates(mapGrid.GridEntityId, Vector2.Zero);
                    PlaceAt(mapGrid, location, _tileDefinitionManager[component.OutputTiles[0]].TileId, component.PlaceTileSound, mapGrid.TileSize / 2f);
                }
            }
        }

        public bool HasBaseTurf(ContentTileDefinition tileDef, string baseTurf)
        {
            foreach (var tileBaseTurf in tileDef.BaseTurfs)
            {
                if (baseTurf == tileBaseTurf)
                    return true;
            }

            return false;
        }

        private void PlaceAt(IMapGrid mapGrid, EntityCoordinates location, ushort tileId, SoundSpecifier placeSound, float offset = 0)
        {
            var variant = _random.Pick(((ContentTileDefinition) _tileDefinitionManager[tileId]).PlacementVariants);
            mapGrid.SetTile(location.Offset(new Vector2(offset, offset)), new Tile(tileId, 0, variant));
            SoundSystem.Play(placeSound.GetSound(), Filter.Pvs(location), location, AudioHelpers.WithVariation(0.125f, _random));
        }
    }
}
