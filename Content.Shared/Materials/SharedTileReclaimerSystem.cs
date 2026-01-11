using System.Numerics;
using Content.Shared.Maps;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Materials;

// SLAM-TODO: Note to self; deleting grid tiles sometimes causes a flashing effect not seen in other similar systems. Figure out why.
public sealed class SharedTileReclaimerSystem : EntitySystem
{

    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly FixtureSystem _fixture = default!;

    private EntityQuery<MapGridComponent> _gridQuery;


    public override void Initialize()
    {
        base.Initialize();

        _gridQuery = GetEntityQuery<MapGridComponent>();
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<TileReclaimerComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var tileReclaimer, out var xform))
        {
            if (tileReclaimer.NextRecycle <= _timing.CurTime)
                Update((uid, tileReclaimer, xform));
        }
    }

    private void Update(Entity<TileReclaimerComponent, TransformComponent> ent)
    {
        var reclaimerGrid = _transform.GetGrid((ent, ent));

        if (reclaimerGrid == null)
            return;

        var fixture = _fixture.GetFixtureOrNull(ent, ent.Comp1.FixtureId);

        if (fixture == null)
            return;

        var shape = fixture.Shape;

        // SLAM-TODO: Replace with system functions + box property + offset in component
        var targetPoint = ent.Comp2.WorldPosition + ent.Comp2.WorldRotation.ToWorldVec();
        var mapPos = _transform.GetMapCoordinates(ent);

        var box = Box2.CenteredAround(targetPoint, new Vector2(1, 1));
        var grids = new List<Entity<MapGridComponent>>();

        if (!(shape is PhysShapeAabb box2))
            return;

        _mapManager.FindGridsIntersecting(mapPos.MapId, _transform.GetWorldMatrix(ent), ref grids);

        var shredded = false;

        Logger.Debug(grids.Count.ToString());

        foreach (var grid in grids)
        {
            if (grid == reclaimerGrid)
                continue;

            // SLAM-TODO: This is probably wrong way to do it
            IEnumerable<TileRef> tiles = default!;

            tiles = _mapSystem.GetLocalTilesIntersecting(grid.Owner, grid.Comp, box2.LocalBounds);

            // SLAM-TODO: This will be checking for wreck grids
            //if (_whitelistSystem.IsWhitelistFail(ent.Comp1.Whitelist, grid) ||
            //    _whitelistSystem.IsWhitelistPass(ent.Comp1.Blacklist, grid))
            //    continue;

            foreach (var tile in tiles)
            {
                foreach (var entityOnTile in _lookup.GetLocalEntitiesIntersecting(tile))
                {
                    _physics.SetCanCollide(entityOnTile, true);
                    _physics.ApplyLinearImpulse(entityOnTile, _physics.GetLinearVelocity(grid.Owner, Transform(entityOnTile).LocalPosition));
                }

                var mapGrid = Comp<MapGridComponent>(tile.GridUid);
                _mapSystem.SetTile(tile.GridUid, mapGrid, tile.GridIndices, Tile.Empty);
                Spawn("SheetSteel1", mapPos); // SLAM-TODO: Replace with proper spawn value
                shredded = true;

                // We suck in the grid slurrrrp
                // SLAM-TODO: Should be set via component, AND also impulses cause stacking speed so that should be tempered
                _physics.ApplyLinearImpulse(grid, -ent.Comp2.WorldRotation.ToWorldVec(), tile.GridIndices);
            }
        }

        if (!shredded)
            return;

        ent.Comp1.NextRecycle = _timing.CurTime + ent.Comp1.RecycleDelay;

        _audio.PlayPredicted(ent.Comp1.Sound, ent, null);
    }
}
