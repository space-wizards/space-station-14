using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Shared.Administration.Logs;
using Content.Shared.Audio;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
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
    [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStackSystem _stackSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TileSystem _tile = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;

    private static readonly Vector2 CheckRange = new(1f, 1f);

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
        var locationMap = _transform.ToMapCoordinates(location);
        if (locationMap.MapId == MapId.Nullspace)
            return;

        var physicQuery = GetEntityQuery<PhysicsComponent>();
        var transformQuery = GetEntityQuery<TransformComponent>();

        var map = _transform.ToMapCoordinates(location);

        // Disallow placement close to grids.
        // FTLing close is okay but this makes alignment too finnicky.
        // While you may already have a tile close you want to replace when we get half-tiles that may also be finnicky
        // so we're just gon with this for now.
        const bool inRange = true;
        var state = (inRange, location.EntityId);
        _mapManager.FindGridsIntersecting(map.MapId, new Box2(map.Position - CheckRange, map.Position + CheckRange), ref state,
            static (EntityUid entityUid, MapGridComponent grid, ref (bool weh, EntityUid EntityId) tuple) =>
            {
                if (tuple.EntityId == entityUid)
                    return true;

                tuple.weh = false;
                return false;
            });

        if (!state.inRange)
        {
            if (_netManager.IsClient && _timing.IsFirstTimePredicted)
                _popup.PopupEntity(Loc.GetString("invalid-floor-placement"), args.User);

            return;
        }

        var userPos = _transform.ToMapCoordinates(transformQuery.GetComponent(args.User).Coordinates).Position;
        var dir = userPos - map.Position;
        var canAccessCenter = false;
        if (dir.LengthSquared() > 0.01)
        {
            var ray = new CollisionRay(map.Position, dir.Normalized(), (int) CollisionGroup.Impassable);
            var results = _physics.IntersectRay(locationMap.MapId, ray, dir.Length(), returnOnFirstHit: true);
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
        TryComp<MapGridComponent>(location.EntityId, out var mapGrid);

        foreach (var currentTile in component.OutputTiles)
        {
            var currentTileDefinition = (ContentTileDefinition) _tileDefinitionManager[currentTile];

            if (mapGrid != null)
            {
                var gridUid = location.EntityId;
                var tile = _map.GetTileRef(gridUid, mapGrid, location);

                if (!CanPlaceTile(gridUid, mapGrid, tile.GridIndices, out var reason))
                {
                    _popup.PopupClient(reason, args.User, args.User);
                    return;
                }

                var baseTurf = (ContentTileDefinition) _tileDefinitionManager[tile.Tile.TypeId];

                if (HasBaseTurf(currentTileDefinition, baseTurf.ID))
                {
                    if (!_stackSystem.Use(uid, 1, stack))
                        continue;

                    PlaceAt(args.User, gridUid, mapGrid, location, currentTileDefinition.TileId, component.PlaceTileSound);
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

                var grid = _mapManager.CreateGridEntity(locationMap.MapId);
                var gridXform = Transform(grid);
                _transform.SetWorldPosition((grid, gridXform), locationMap.Position);
                location = new EntityCoordinates(grid, Vector2.Zero);
                PlaceAt(args.User, grid, grid.Comp, location, _tileDefinitionManager[component.OutputTiles[0]].TileId, component.PlaceTileSound, grid.Comp.TileSize / 2f);
                return;
            }
        }
    }

    public bool HasBaseTurf(ContentTileDefinition tileDef, string baseTurf)
    {
        return tileDef.BaseTurf == baseTurf;
    }

    private void PlaceAt(EntityUid user, EntityUid gridUid, MapGridComponent mapGrid, EntityCoordinates location,
        ushort tileId, SoundSpecifier placeSound, float offset = 0)
    {
        _adminLogger.Add(LogType.Tile, LogImpact.Low, $"{ToPrettyString(user):actor} placed tile {_tileDefinitionManager[tileId].Name} at {ToPrettyString(gridUid)} {location}");

        var random = new System.Random((int) _timing.CurTick.Value);
        var variant = _tile.PickVariant((ContentTileDefinition) _tileDefinitionManager[tileId], random);
        _map.SetTile(gridUid, mapGrid,location.Offset(new Vector2(offset, offset)), new Tile(tileId, 0, variant));

        _audio.PlayPredicted(placeSound, location, user);
    }

    public bool CanPlaceTile(EntityUid gridUid, MapGridComponent component, Vector2i gridIndices, [NotNullWhen(false)] out string? reason)
    {
        var ev = new FloorTileAttemptEvent(gridIndices);
        RaiseLocalEvent(gridUid, ref ev);

        if (ev.Cancelled)
        {
            reason = Loc.GetString("invalid-floor-placement");
            return false;
        }

        reason = null;
        return true;
    }
}
