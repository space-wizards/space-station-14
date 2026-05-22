using Content.Shared.Conveyor;
using Content.Shared.Maps;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Materials;

/// <summary>
/// Handles interactions and logic related to <see cref="TileReclaimerComponent"/>.
/// </summary>
public abstract partial class SharedTileReclaimerSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedMapSystem _mapSystem = default!;
    [Dependency] private IMapManager _mapManager = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private TurfSystem _turf = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TileReclaimerComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<TileReclaimerComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextRecycle = _timing.CurTime;
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<TileReclaimerComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var tileReclaimer, out var xform))
        {
            if (tileReclaimer.NextRecycle > _timing.CurTime)
                return;

            Update((uid, tileReclaimer, xform));
        }
    }

    private void Update(Entity<TileReclaimerComponent, TransformComponent> ent)
    {
        ent.Comp1.NextRecycle += ent.Comp1.RecycleDelay;

        //TODO: The recycler is hardcoded to rely on the conveyor component for its powered state, and this system is based on the same functionality. That should be fixed into a more general solution for both systems, but for the sake of consistency I'm not doing that now.
        if (!TryComp<ConveyorComponent>(ent, out var conveyor) || conveyor.State == ConveyorState.Off)
            return;

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

            if (!_whitelist.CheckBoth(grid, ent.Comp1.Blacklist, ent.Comp1.Whitelist))
                continue;

            var intersectingEntities = new HashSet<EntityUid>();

            foreach (var tile in _mapSystem.GetTilesIntersecting(grid.Owner, grid.Comp, box))
            {
                var tileDef = _turf.GetContentTileDefinition(tile);
                if (tileDef.Indestructible)
                    continue;

                intersectingEntities.Clear();
                _lookup.GetLocalEntitiesIntersecting(tile.GridUid, tile.GridIndices, intersectingEntities);

                foreach (var entityOnTile in intersectingEntities)
                {
                    _physics.SetCanCollide(entityOnTile, true);
                    _physics.ApplyLinearImpulse(entityOnTile, _physics.GetLinearVelocity(grid.Owner, Transform(entityOnTile).LocalPosition));
                }

                _mapSystem.SetTile(tile.GridUid, grid, tile.GridIndices, Tile.Empty);
                SpawnMaterialsFromComposition((ent, null, ent.Comp2), tileDef, ent.Comp1.Efficiency);
                shredded = true;

                // We suck in the grid slurrrrp
                // TODO: This can probably be refined, as it applies a stacking speed which could theoretically get out of hand if a grid is very long.
                _physics.ApplyLinearImpulse(grid, ent.Comp1.SlurpStrength * _transform.GetWorldRotation(ent.Comp2).ToWorldVec(), tile.GridIndices);
            }
        }

        if (!shredded)
            return;

        _audio.PlayPvs(ent.Comp1.Sound, ent);
    }

    /// <summary>
    /// Spawns materials from the <see cref="ContentTileDefinition.MaterialComposition"/> of the given tile.
    /// </summary>
    /// <param name="ent">Entity performing the spawning action.</param>
    /// <param name="tileDefinition">Tile definition of the tile being reclaimed.</param>
    /// <param name="efficiency">Multiplier of the material amount.</param>
    protected virtual void SpawnMaterialsFromComposition(Entity<MaterialStorageComponent?, TransformComponent?> ent,
        ContentTileDefinition tileDefinition,
        float efficiency)
    {
        // Handled on the server because that's where MaterialStorageSystem is.
    }
}
