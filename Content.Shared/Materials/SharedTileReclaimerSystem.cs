using System.Numerics;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
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

        // SLAM-TODO: Replace with system functions + box property + offset in component
        var targetPoint = ent.Comp2.WorldPosition + ent.Comp2.WorldRotation.ToWorldVec() * 0.625f;
        var mapPos = _transform.GetMapCoordinates(ent);

        var range = 0.125f;

        var box = Box2.CenteredAround(targetPoint, new Vector2(range * 2.5f, range));
        var grids = new List<Entity<MapGridComponent>>();

        _mapManager.FindGridsIntersecting(mapPos.MapId, box, ref grids);

        var tiles = new List<(Vector2i, Tile)>();

        foreach (var grid in grids)
        {
            if (grid == reclaimerGrid)
                continue;

            // SLAM-TODO: This will be checking for wreck grids
            //if (_whitelistSystem.IsWhitelistFail(ent.Comp1.Whitelist, grid) ||
            //    _whitelistSystem.IsWhitelistPass(ent.Comp1.Blacklist, grid))
            //    continue;

            foreach (var tile in _mapSystem.GetTilesIntersecting(grid.Owner, grid.Comp, box))
            {
                tiles.Add((tile.GridIndices, Tile.Empty));
                Spawn("SheetSteel1", mapPos); // SLAM-TODO: Replace with proper spawn value
            }

            _mapSystem.SetTiles(grid, grid, tiles);
        }

        if (tiles.Count == 0)
            return;

        ent.Comp1.NextRecycle = _timing.CurTime + ent.Comp1.RecycleDelay;

        _audio.PlayPredicted(ent.Comp1.Sound, ent, null);
    }
}
