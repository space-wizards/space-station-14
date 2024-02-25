using System.Linq;
using Content.Shared.Blob.Components;
using Content.Shared.Physics;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Blob;

/// <summary>
/// This handles logic related to the blob's movement, abilities, minions, and spreading.
/// </summary>
public sealed class SharedBlobSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly FixtureSystem _fixture = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<BlobStructureComponent> _blobStructureQuery;
    private EntityQuery<TransformComponent> _xformQuery;
    private EntityQuery<FixturesComponent> _fixtureQuery;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent((Entity<BlobStructureComponent> ent, ref ComponentInit _) => UpdateNearby(ent));
        //bug: this doesn't actually work when it's deleted
        SubscribeLocalEvent((Entity<BlobStructureComponent> ent, ref EntParentChangedMessage _) => UpdateNearby(ent));
        SubscribeLocalEvent<BlobStructureComponent, PreventCollideEvent>(OnPreventCollide);

        _blobStructureQuery = GetEntityQuery<BlobStructureComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();
        _fixtureQuery = GetEntityQuery<FixturesComponent>();
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
        foreach (var blob in _lookup.GetEntitiesInRange<BlobStructureComponent>(xform.Comp.Coordinates, 3.5f))
        {
            if (TerminatingOrDeleted(blob))
                continue;

            if (!_fixtureQuery.TryGetComponent(blob, out var body) ||
                !_xformQuery.TryGetComponent(blob, out var blobXform))
                continue;

            UpdateBlobStructureFixtures((blob.Owner, body, blobXform));
        }
    }

    private void UpdateBlobStructureFixtures(Entity<FixturesComponent, TransformComponent> ent)
    {
        var manager = ent.Comp1;
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
        var neighbors = GetOrthogonalNeighborCells(grid, tile.GridIndices + offset);
        var tileBlocked = !neighbors.Any(_blobStructureQuery.HasComponent);
        if (_fixture.GetFixtureOrNull(ent, name, ent) is { } fixture)
            _physics.SetHard(ent, fixture, tileBlocked, ent);
    }

    //todo this needs to be in engine once im not lazy.
    public IEnumerable<EntityUid> GetOrthogonalNeighborCells(Entity<MapGridComponent> ent, Vector2i position)
    {
        // ReSharper disable EnforceForeachStatementBraces
        foreach (var cell in _map.GetAnchoredEntities(ent, ent, position))
            yield return cell;
        foreach (var cell in _map.GetAnchoredEntities(ent, ent, position + new Vector2i(0, 1)))
            yield return cell;
        foreach (var cell in _map.GetAnchoredEntities(ent, ent, position + new Vector2i(0, -1)))
            yield return cell;
        foreach (var cell in _map.GetAnchoredEntities(ent, ent, position + new Vector2i(1, 0)))
            yield return cell;
        foreach (var cell in _map.GetAnchoredEntities(ent, ent, position + new Vector2i(-1, 0)))
            yield return cell;
        foreach (var cell in _map.GetAnchoredEntities(ent, ent, position + new Vector2i(1, 1)))
            yield return cell;
        foreach (var cell in _map.GetAnchoredEntities(ent, ent, position + new Vector2i(-1, 1)))
            yield return cell;
        foreach (var cell in _map.GetAnchoredEntities(ent, ent, position + new Vector2i(1, -1)))
            yield return cell;
        foreach (var cell in _map.GetAnchoredEntities(ent, ent, position + new Vector2i(-1, -1)))
            yield return cell;
        // ReSharper restore EnforceForeachStatementBraces
    }
}
