using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Blob.Components;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Input;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Blob;

/// <summary>
/// This handles logic related to the blob's movement, abilities, minions, and spreading.
/// </summary>
public abstract class SharedBlobSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly FixtureSystem _fixture = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    protected EntityQuery<BlobStructureComponent> BlobStructureQuery;
    private EntityQuery<TransformComponent> _xformQuery;
    private EntityQuery<FixturesComponent> _fixtureQuery;

    [ValidatePrototypeId<TagPrototype>]
    public const string AllowBlobReplaceTag = "BlobAllowReplace";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<BlobMarkerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BlobMarkerComponent, BlobCreateStructureEvent>(OnCreateStructure);

        SubscribeLocalEvent((Entity<BlobStructureComponent> ent, ref ComponentInit _) => UpdateNearby(ent));
        //bug: this doesn't actually work when it's deleted
        SubscribeLocalEvent((Entity<BlobStructureComponent> ent, ref EntParentChangedMessage _) => UpdateNearby(ent));
        SubscribeLocalEvent<BlobStructureComponent, PreventCollideEvent>(OnPreventCollide);

        CommandBinds.Builder
            .Bind(EngineKeyFunctions.Use,
                new PointerInputCmdHandler(HandleAttemptPlaceBlob))
            .Bind(ContentKeyFunctions.TryPullObject, //todo this needs it's own keybind at some point instead of being TryPull
                new PointerInputCmdHandler(HandleAttemptUpgrade))
            .Register<SharedBlobSystem>();

        BlobStructureQuery = GetEntityQuery<BlobStructureComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();
        _fixtureQuery = GetEntityQuery<FixturesComponent>();
    }
    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<SharedBlobSystem>();
    }

    private void OnMapInit(Entity<BlobMarkerComponent> ent, ref MapInitEvent args)
    {
        var comp = ent.Comp;

        if (TryComp<ActionsComponent>(ent, out var actions))
        {
            foreach (var action in comp.Actions)
            {
                _actions.AddAction(ent, action, component: actions);
            }
        }

        SpawnBlobCreated(comp.CoreProtoId, Transform(ent).Coordinates, ent);
    }

    private void OnCreateStructure(Entity<BlobMarkerComponent> ent, ref BlobCreateStructureEvent args)
    {
        var pos = Transform(ent).Coordinates;
        if (!TryGetBlobStructure(pos, out var blob) || !_tag.HasTag(blob.Value, AllowBlobReplaceTag))
            return;

        var rangeComp = EntityManager.ComponentFactory.GetRegistration(args.RangeComponent).Type;
        var nearby = _lookup.GetEntitiesInRange(rangeComp, pos.SnapToGrid().ToMap(EntityManager, _transform), args.MinRange);

        if (nearby.Count != 0)
        {
            _popup.PopupClient(Loc.GetString("blob-popup-structure-too-close"), ent, ent);
            return;
        }

        if (!TryUseResource((ent, ent), args.Cost))
        {
            _popup.PopupClient(Loc.GetString("blob-popup-structure-no-resource"), ent, ent);
            return;
        }

        if (_net.IsServer)
        {
            Del(blob);
            SpawnBlobCreated(args.Structure, pos, ent);
        }
    }

    private void OnPreventCollide(Entity<BlobStructureComponent> ent, ref PreventCollideEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.OurFixture.CollisionLayer != (int) CollisionGroup.GhostImpassable)
            return;

        if (HasComp<BlobMarkerComponent>(args.OtherEntity))
            return;

        args.Cancelled = true;
    }

    public void UpdateNearby(Entity<BlobStructureComponent> ent)
    {
        var xform = _xformQuery.Get(ent);
        foreach (var blob in _lookup.GetEntitiesInRange<BlobStructureComponent>(xform.Comp.Coordinates, 4f))
        {
            if (TerminatingOrDeleted(blob))
                continue;

            if (!_fixtureQuery.TryGetComponent(blob, out var body) ||
                !_xformQuery.TryGetComponent(blob, out var blobXform))
                continue;

            UpdateBlobStructureFixtures((blob.Owner, body, blobXform));
        }
    }

    private bool HandleAttemptPlaceBlob(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
    {
        if (session?.AttachedEntity is not { } playerEnt)
            return false;

        if (!TryComp<BlobMarkerComponent>(playerEnt, out var blobMarker))
            return false;

        if (!coords.TryGetTileRef(out var tile, EntityManager) ||
            _turf.IsTileBlocked(tile.Value, CollisionGroup.MidImpassable))
        {
            // todo in this scenario, make an adjacent blob wall "attack" this tile at cost of 2 resource.
            return false;
        }

        if (HasBlobStructure(coords))
            return false;

        if (!HasAdjacentBlobStructures(coords, includeSelf: false, getDiagonal: false))
            return false;

        if (!TryUseResource((playerEnt, blobMarker), blobMarker.RegularBlobCost))
            return false;

        SpawnBlobCreated(blobMarker.RegularBlobProtoId, coords, (playerEnt, blobMarker));
        return _net.IsServer;
    }

    private bool HandleAttemptUpgrade(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
    {
        if (session?.AttachedEntity is not { } playerEnt)
            return false;

        if (!TryComp<BlobMarkerComponent>(playerEnt, out var blobMarker))
            return false;

        if (!TryGetBlobStructure(coords, out var ent) ||
            !TryComp<BlobUpgradeableComponent>(ent, out var upgradeableComponent))
            return false;

        if (!TryUseResource((playerEnt, blobMarker), upgradeableComponent.UpgradeCost))
        {
            _popup.PopupClient(Loc.GetString("blob-popup-structure-no-resource"), playerEnt, playerEnt);
            return false;
        }

        if (_net.IsServer)
        {
            Del(ent);
            SpawnBlobCreated(upgradeableComponent.UpgradeEntity, coords, (playerEnt, blobMarker));
        }
        return _net.IsServer;
    }

    private void UpdateBlobStructureFixtures(Entity<FixturesComponent, TransformComponent> ent)
    {
        var xform = ent.Comp2;

        if (xform.GridUid is not { } grid ||
            !TryComp<MapGridComponent>(grid, out var gridComp) ||
            !_map.TryGetTileRef(grid, gridComp, xform.Coordinates, out var tileRef))
            return;

        UpdateFixtureHard(ent, (grid, gridComp), tileRef, new Vector2i(0, 2), "blobNorth");
        UpdateFixtureHard(ent, (grid, gridComp), tileRef, new Vector2i(0, -2), "blobSouth");
        UpdateFixtureHard(ent, (grid, gridComp), tileRef, new Vector2i(2, 0), "blobEast");
        UpdateFixtureHard(ent, (grid, gridComp), tileRef, new Vector2i(-2, 0), "blobWest");
        UpdateFixtureHard(ent, (grid, gridComp), tileRef, new Vector2i(1, 2), "blobNorthEast");
        UpdateFixtureHard(ent, (grid, gridComp), tileRef, new Vector2i(-1, 2), "blobNorthWest");
        UpdateFixtureHard(ent, (grid, gridComp), tileRef, new Vector2i(1, -2), "blobSouthEast");
        UpdateFixtureHard(ent, (grid, gridComp), tileRef, new Vector2i(-1, -2), "blobSouthWest");
        UpdateFixtureHard(ent, (grid, gridComp), tileRef, new Vector2i(2, 1), "blobEastNorth");
        UpdateFixtureHard(ent, (grid, gridComp), tileRef, new Vector2i(2, -1), "blobEastSouth");
        UpdateFixtureHard(ent, (grid, gridComp), tileRef, new Vector2i(-2, 1), "blobWestNorth");
        UpdateFixtureHard(ent, (grid, gridComp), tileRef, new Vector2i(-2, -1), "blobWestSouth");
    }

    private void UpdateFixtureHard(Entity<FixturesComponent, TransformComponent> ent, Entity<MapGridComponent> grid, TileRef tile, Vector2i offset, string name)
    {
        if (_fixture.GetFixtureOrNull(ent, name, ent) is { } fixture)
            _physics.SetHard(ent, fixture, !HasAdjacentBlobStructures(grid, tile.GridIndices + offset), ent);
    }

    public bool HasAdjacentBlobStructures(EntityCoordinates coords, bool includeSelf = true, bool getDiagonal = true)
    {
        var grid = coords.GetGridUid(EntityManager);
        if (!TryComp<MapGridComponent>(grid, out var gridComp))
            return false;

        if (!_map.TryGetTileRef(grid.Value, gridComp, coords, out var tile))
            return false;

        return HasAdjacentBlobStructures((grid.Value, gridComp), tile.GridIndices, includeSelf, getDiagonal);
    }

    public bool HasAdjacentBlobStructures(Entity<MapGridComponent> grid, Vector2i indices, bool includeSelf = true, bool getDiagonal = true)
    {
        var neighbors = GetOrthogonalNeighborCells(grid, indices, includeSelf, getDiagonal);
        return neighbors.Any(BlobStructureQuery.HasComponent);
    }

    public bool HasBlobStructure(EntityCoordinates coords)
    {
        return TryGetBlobStructure(coords, out _);
    }

    public bool TryGetBlobStructure(EntityCoordinates coords, [NotNullWhen(true)] out Entity<BlobStructureComponent>? ent)
    {
        ent = null;

        var grid = coords.GetGridUid(EntityManager);
        if (!TryComp<MapGridComponent>(grid, out var gridComp))
            return false;

        if (!_map.TryGetTileRef(grid.Value, gridComp, coords, out var tile))
            return false;

        foreach (var cell in _map.GetAnchoredEntities(grid.Value, gridComp, tile.GridIndices))
        {
            if (!BlobStructureQuery.TryGetComponent(cell, out var comp))
                continue;

            ent = (cell, comp);
            return true;
        }

        return false;
    }

    //todo this needs to be in engine once im not lazy.
    public IEnumerable<EntityUid> GetOrthogonalNeighborCells(Entity<MapGridComponent> ent, Vector2i position, bool includeSelf = true, bool getDiagonal = true)
    {
        // ReSharper disable EnforceForeachStatementBraces
        if (includeSelf)
        {
            foreach (var cell in _map.GetAnchoredEntities(ent, ent, position))
                yield return cell;
        }
        foreach (var cell in _map.GetAnchoredEntities(ent, ent, position + new Vector2i(0, 1)))
            yield return cell;
        foreach (var cell in _map.GetAnchoredEntities(ent, ent, position + new Vector2i(0, -1)))
            yield return cell;
        foreach (var cell in _map.GetAnchoredEntities(ent, ent, position + new Vector2i(1, 0)))
            yield return cell;
        foreach (var cell in _map.GetAnchoredEntities(ent, ent, position + new Vector2i(-1, 0)))
            yield return cell;
        if (getDiagonal)
        {
            foreach (var cell in _map.GetAnchoredEntities(ent, ent, position + new Vector2i(1, 1)))
                yield return cell;
            foreach (var cell in _map.GetAnchoredEntities(ent, ent, position + new Vector2i(-1, 1)))
                yield return cell;
            foreach (var cell in _map.GetAnchoredEntities(ent, ent, position + new Vector2i(1, -1)))
                yield return cell;
            foreach (var cell in _map.GetAnchoredEntities(ent, ent, position + new Vector2i(-1, -1)))
                yield return cell;
        }
        // ReSharper restore EnforceForeachStatementBraces
    }

    public EntityUid? SpawnBlobCreated(EntProtoId proto, EntityCoordinates position, Entity<BlobMarkerComponent> creator)
    {
        //todo remove when predicted entity spawning
        if (_net.IsClient)
            return null;

        var ent = Spawn(proto, position);
        var comp = EnsureComp<BlobCreatedComponent>(ent);
        comp.Creator = creator;
        Dirty(ent, comp);
        return ent;
    }

    public bool TryUseResource(Entity<BlobMarkerComponent?> ent, int amount)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (ent.Comp.Resource - amount < 0)
            return false;

        SetResource((ent, ent.Comp), ent.Comp.Resource - amount);
        return true;
    }

    public bool TryAddResource(Entity<BlobMarkerComponent?> ent, int amount)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        var realAmount = Math.Min(ent.Comp.Resource + amount, ent.Comp.ResourceMax);

        SetResource((ent, ent.Comp), realAmount);
        return true;
    }

    public bool HasResource(Entity<BlobMarkerComponent?> ent, int amount)
    {
        return Resolve(ent, ref ent.Comp) && ent.Comp.Resource >= amount;
    }

    public void SetResource(Entity<BlobMarkerComponent> ent, int amount)
    {
        ent.Comp.Resource = amount;
        Dirty(ent, ent.Comp);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<BlobMarkerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime < comp.NextSecond)
                continue;
            comp.NextSecond += TimeSpan.FromSeconds(1);
            TryAddResource((uid, comp), comp.ResourcePassiveGen);
        }
    }
}
