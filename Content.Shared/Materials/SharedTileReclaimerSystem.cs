using Content.Shared.Conveyor;
using Content.Shared.Maps;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Materials;

// SLAM-TODO: Note to self; deleting grid tiles sometimes causes a flashing effect not seen in other similar systems. Figure out why.
public abstract class SharedTileReclaimerSystem : EntitySystem
{

    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _powerReceiver = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly TurfSystem _turf = default!;


    public override void Initialize()
    {
        base.Initialize();
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
        if (!TryComp<ConveyorComponent>(ent, out var conveyor) || conveyor.State == ConveyorState.Off)
            return; // SLAM-TODO: The recycler is hardcoded to rely on the conveyor component for its powered state. That should be fixed into a more general solution, but for the sake of prototyping I'm not doing that now.

        var reclaimerGrid = _transform.GetGrid((ent, ent));

        if (reclaimerGrid == null)
            return;

        var mapPos = _transform.GetMapCoordinates(ent);

        var grids = new List<Entity<MapGridComponent>>();

        var entMatrix = _transform.GetWorldMatrix(ent.Owner);
        var box = entMatrix.TransformBox(ent.Comp1.RecyclingBox);

        _mapManager.FindGridsIntersecting(mapPos.MapId, box, ref grids);

        var shredded = false;

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
                var tileDef = _turf.GetContentTileDefinition(tile);
                if (tileDef.Indestructible)
                    continue;

                foreach (var entityOnTile in _lookup.GetLocalEntitiesIntersecting(tile))
                {
                    _physics.SetCanCollide(entityOnTile, true);
                    _physics.ApplyLinearImpulse(entityOnTile, _physics.GetLinearVelocity(grid.Owner, Transform(entityOnTile).LocalPosition));
                }

                var mapGrid = Comp<MapGridComponent>(tile.GridUid);
                _mapSystem.SetTile(tile.GridUid, mapGrid, tile.GridIndices, Tile.Empty);
                SpawnMaterialsFromComposition(ent, tileDef, ent.Comp1.Efficiency, null, ent.Comp2);
                shredded = true;

                // We suck in the grid slurrrrp
                // SLAM-TODO: Should be set via component, AND also impulses cause stacking speed so that should be tempered
                _physics.ApplyLinearImpulse(grid, ent.Comp1.SlurpStrength * _transform.GetWorldRotation(ent.Comp2).ToWorldVec(), tile.GridIndices);
            }
        }

        if (!shredded)
            return;

        ent.Comp1.NextRecycle = _timing.CurTime + ent.Comp1.RecycleDelay;

        _audio.PlayPredicted(ent.Comp1.Sound, ent, null);
    }

    protected virtual void SpawnMaterialsFromComposition(EntityUid reclaimer,
        ContentTileDefinition tileDefinition,
        float efficiency,
        MaterialStorageComponent? storage = null,
        TransformComponent? xform = null)
    {
        // Handled on the server because that's where MaterialStorageSystem is.
    }
}
