using System.Linq;
using Content.Shared.Audio;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Tiles;

public sealed class FloorTileSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStackSystem _stackSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FloorTileComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnAfterInteract(EntityUid uid, FloorTileComponent component, AfterInteractEvent args)
    {
        if (!args.CanReach || args.Handled)
            return;

        if (!TryComp<StackComponent>(uid, out var stack))
            return;

        if (component.OutputTiles == null)
            return;

        // this looks a bit sussy but it might be because it needs to be able to place off of grids and expand them
        var location = args.ClickLocation.AlignWithClosestGridTile();
        var locationMap = location.ToMap(EntityManager, _transform);
        if (locationMap.MapId == MapId.Nullspace)
            return;

        var physicQuery = GetEntityQuery<PhysicsComponent>();
        var transformQuery = GetEntityQuery<TransformComponent>();

        var tilePos = location.ToMapPos(EntityManager, _transform);
        var userPos = transformQuery.GetComponent(args.User).Coordinates.ToMapPos(EntityManager, _transform);
        var dir = userPos - tilePos;
        var canAccessCenter = false;
        if (dir.LengthSquared > 0.01)
        {
            var ray = new CollisionRay(tilePos, dir.Normalized, (int) CollisionGroup.Impassable);
            var results = _physics.IntersectRay(locationMap.MapId, ray, dir.Length, returnOnFirstHit: true);
            canAccessCenter = !results.Any();
        }

        // if user can access tile center then they can place floor
        // otherwise check it isn't blocked by a wall
        if (!canAccessCenter)
        {
            foreach (var ent in location.GetEntitiesInTile(lookupSystem: _lookup))
            {
                if (physicQuery.TryGetComponent(ent, out var phys) &&
                    phys.BodyType == BodyType.Static &&
                    phys.Hard &&
                    (phys.CollisionLayer & (int) CollisionGroup.Impassable) != 0)
                {
                    return;
                }
            }
        }
        _mapManager.TryGetGrid(location.EntityId, out var mapGrid);

        foreach (var currentTile in component.OutputTiles)
        {
            var currentTileDefinition = (ContentTileDefinition) _tileDefinitionManager[currentTile];

            if (mapGrid != null)
            {
                var ev = new FloorTileAttemptEvent();
                RaiseLocalEvent(mapGrid);

                if (HasComp<ProtectedGridComponent>(mapGrid.Owner) || ev.Cancelled)
                {
                    if (_netManager.IsClient && _timing.IsFirstTimePredicted)
                        _popup.PopupEntity(Loc.GetString("invalid-floor-placement"), args.User);

                    return;
                }

                var tile = mapGrid.GetTileRef(location);
                var baseTurf = (ContentTileDefinition) _tileDefinitionManager[tile.Tile.TypeId];

                if (HasBaseTurf(currentTileDefinition, baseTurf.ID))
                {
                    if (!_stackSystem.Use(uid, 1, stack))
                        continue;

                    PlaceAt(args.User, mapGrid, location, currentTileDefinition.TileId, component.PlaceTileSound);
                    args.Handled = true;
                    return;
                }
            }
            else if (HasBaseTurf(currentTileDefinition, ContentTileDefinition.SpaceID))
            {
                if (!_stackSystem.Use(uid, 1, stack))
                    continue;

                args.Handled = true;
                if (_netManager.IsClient)
                    return;

                mapGrid = _mapManager.CreateGrid(locationMap.MapId);
                var gridXform = Transform(mapGrid.Owner);
                _transform.SetWorldPosition(gridXform, locationMap.Position);
                location = new EntityCoordinates(mapGrid.Owner, Vector2.Zero);
                PlaceAt(args.User, mapGrid, location, _tileDefinitionManager[component.OutputTiles[0]].TileId, component.PlaceTileSound, mapGrid.TileSize / 2f);
                return;
            }
        }
    }

    public bool HasBaseTurf(ContentTileDefinition tileDef, string baseTurf)
    {
        return tileDef.BaseTurf == baseTurf;
    }

    private void PlaceAt(EntityUid user, MapGridComponent mapGrid, EntityCoordinates location, ushort tileId, SoundSpecifier placeSound, float offset = 0)
    {
        var variant = _random.Pick(((ContentTileDefinition) _tileDefinitionManager[tileId]).PlacementVariants);
        mapGrid.SetTile(location.Offset(new Vector2(offset, offset)), new Tile(tileId, 0, variant));

        _audio.PlayPredicted(placeSound, location, user, AudioHelpers.WithVariation(0.125f, _random));
    }
}
