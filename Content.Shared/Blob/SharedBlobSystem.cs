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
public abstract partial class SharedBlobSystem : EntitySystem
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
        InitializeNode();
        InitializeResource();

        SubscribeLocalEvent<BlobOvermindComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BlobOvermindComponent, BlobCreateStructureEvent>(OnCreateStructure);
        SubscribeLocalEvent<BlobOvermindComponent, BlobJumpToCoreEvent>(OnJumpToCore);
        SubscribeLocalEvent<BlobOvermindComponent, BlobSwapCoreEvent>(OnSwapCore);

        //todo this needs to reload the fixtures for blob structures when they re-enter PVS
        SubscribeLocalEvent((Entity<BlobStructureComponent> ent, ref ComponentStartup _) => UpdateNearby(ent));
        SubscribeLocalEvent<BlobStructureComponent, MoveEvent>(OnStructureMove);
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

    private void OnMapInit(Entity<BlobOvermindComponent> ent, ref MapInitEvent args)
    {
        var comp = ent.Comp;

        comp.NextSecond = _timing.CurTime + TimeSpan.FromSeconds(1);

        if (TryComp<ActionsComponent>(ent, out var actions))
        {
            foreach (var action in comp.Actions)
            {
                _actions.AddAction(ent, action, component: actions);
            }
        }

        SpawnBlobCreated(comp.CoreProtoId, Transform(ent).Coordinates, ent);
        Dirty(ent, ent.Comp);
    }

    private void OnCreateStructure(Entity<BlobOvermindComponent> ent, ref BlobCreateStructureEvent args)
    {
        if (args.Handled)
            return;

        var pos = Transform(ent).Coordinates;
        //todo popups for both of these conditions separately
        if (!TryGetBlobStructure(pos, out var blob) || !_tag.HasTag(blob.Value, AllowBlobReplaceTag))
            return;

        var mapPos = pos.SnapToGrid().ToMap(EntityManager, _transform);

        var rangeComp = EntityManager.ComponentFactory.GetRegistration(args.RangeComponent).Type;
        var nearby = _lookup.GetEntitiesInRange(rangeComp, mapPos, args.MinRange);

        if (nearby.Count != 0)
        {
            _popup.PopupClient(Loc.GetString("blob-popup-structure-too-close"), ent, ent);
            return;
        }

        if (args.RequiresNode)
        {
            if (!HasNodeNearby(pos.SnapToGrid()))
            {
                _popup.PopupClient(Loc.GetString("blob-popup-structure-no-nodes"), ent, ent);
                return;
            }
        }

        if (!TryUseResource((ent, ent), args.Cost, user: ent))
            return;

        if (_net.IsServer)
        {
            Del(blob);
            foreach (var structure in args.Structure)
            {
                SpawnBlobCreated(structure, pos, ent);
            }
        }

        args.Handled = true;
    }

    private void OnJumpToCore(Entity<BlobOvermindComponent> ent, ref BlobJumpToCoreEvent args)
    {
        if (args.Handled)
            return;

        if (!TryGetCore(ent, out var core))
            return;

        _transform.SetCoordinates(ent, Transform(core.Value).Coordinates);
        args.Handled = true;
    }

    private void OnSwapCore(Entity<BlobOvermindComponent> ent, ref BlobSwapCoreEvent args)
    {
        if (args.Handled)
            return;

        if (!TryGetCore(ent, out var core))
            return;

        if (!TryGetBlobStructure(Transform(ent).Coordinates, out var hovering))
        {
            _popup.PopupClient(Loc.GetString("blob-popup-swap-no-node"), ent, ent);
            return;
        }

        if (core.Value.Owner == hovering.Value.Owner)
            return;

        if (!HasComp<BlobNodeComponent>(hovering))
        {
            _popup.PopupClient(Loc.GetString("blob-popup-swap-no-node"), ent, ent);
            return;
        }

        if (!TryUseResource((ent, ent), ent.Comp.SwapCoreCost, ent))
            return;

        var coreXform = Transform(core.Value);
        var hoverXform = Transform(hovering.Value);
        var pos2 = coreXform.Coordinates;
        var pos1 = hoverXform.Coordinates;
        _transform.SetCoordinates(core.Value, coreXform, pos1);
        _transform.SetCoordinates(hovering.Value, hoverXform, pos2);
        _transform.AnchorEntity((core.Value, coreXform));
        _transform.AnchorEntity((hovering.Value, hoverXform));
        UpdateNearby(core.Value, coreXform.Coordinates);
        UpdateNearby(hovering.Value, hoverXform.Coordinates);
        args.Handled = true;
    }

    private void OnStructureMove(Entity<BlobStructureComponent> ent, ref MoveEvent args)
    {
        if (!TerminatingOrDeleted(ent))
            SetBlobStructurePulsed(ent, HasNodeNearby(args.NewPosition));
        UpdateNearby(ent, args.OldPosition);
    }

    private void OnPreventCollide(Entity<BlobStructureComponent> ent, ref PreventCollideEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.OurFixture.CollisionLayer != (int) CollisionGroup.GhostImpassable)
            return;

        if (HasComp<BlobOvermindComponent>(args.OtherEntity))
            return;

        args.Cancelled = true;
    }

    public void UpdateNearby(EntityUid ent, EntityCoordinates? coords = null)
    {
        coords ??= _xformQuery.Get(ent).Comp.Coordinates;
        foreach (var blob in _lookup.GetEntitiesInRange<BlobStructureComponent>(coords.Value, 4f))
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

        if (!TryComp<BlobOvermindComponent>(playerEnt, out var blobMarker))
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

        if (!TryUseResource((playerEnt, blobMarker), blobMarker.RegularBlobCost, playerEnt))
            return false;

        SpawnBlobCreated(blobMarker.RegularBlobProtoId, coords, (playerEnt, blobMarker));
        return _net.IsServer;
    }

    private bool HandleAttemptUpgrade(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
    {
        if (session?.AttachedEntity is not { } playerEnt)
            return false;

        if (!TryComp<BlobOvermindComponent>(playerEnt, out var blobMarker))
            return false;

        if (!TryGetBlobStructure(coords, out var ent) ||
            !TryComp<BlobUpgradeableComponent>(ent, out var upgradeableComponent))
            return false;

        if (!TryUseResource((playerEnt, blobMarker), upgradeableComponent.UpgradeCost, playerEnt))
            return false;

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

        Dirty(ent, ent.Comp1);
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

    public EntityUid? SpawnBlobCreated(EntProtoId proto, EntityCoordinates position, Entity<BlobOvermindComponent> creator)
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

    public bool TryGetCore(
        Entity<BlobOvermindComponent> ent,
        [NotNullWhen(true)] out Entity<BlobCoreComponent, BlobCreatedComponent>? blob)
    {
        var query = EntityQueryEnumerator<BlobCoreComponent, BlobCreatedComponent>();
        while (query.MoveNext(out var uid, out var core, out var created))
        {
            if (created.Creator != ent)
                continue;

            blob = (uid, core, created);
            return true;
        }

        blob = null;
        return false;
    }

    public bool TryUseResource(Entity<BlobOvermindComponent?> ent, int amount, EntityUid? user = null, bool silent = false)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (!HasResource(ent, amount))
        {
            if (user != null && !silent)
                _popup.PopupClient(Loc.GetString("blob-popup-structure-no-resource", ("amount", amount - ent.Comp.Resource)), user.Value, user.Value);
            return false;
        }

        SetResource((ent, ent.Comp), ent.Comp.Resource - amount);
        return true;
    }

    public bool TryAddResource(Entity<BlobOvermindComponent?> ent, int amount)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        var realAmount = Math.Min(ent.Comp.Resource + amount, ent.Comp.ResourceMax);

        SetResource((ent, ent.Comp), realAmount);
        return true;
    }

    public bool HasResource(Entity<BlobOvermindComponent?> ent, int amount)
    {
        return Resolve(ent, ref ent.Comp) && ent.Comp.Resource >= amount;
    }

    public void SetResource(Entity<BlobOvermindComponent> ent, int amount)
    {
        ent.Comp.Resource = amount;
        Dirty(ent, ent.Comp);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateResource();

        var query = EntityQueryEnumerator<BlobOvermindComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime < comp.NextSecond)
                continue;
            comp.NextSecond += TimeSpan.FromSeconds(1);
            TryAddResource((uid, comp), comp.ResourcePassiveGen);
        }
    }
}
