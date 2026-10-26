using System.Linq;
using System.Numerics;
using Content.Server.Anomaly.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Destructible;
using Content.Server.Tiles;
using Content.Shared.Administration.Logs;
using Content.Shared.Anomaly.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Database;
using Content.Shared.Decals;
using Content.Shared.Item;
using Content.Shared.Maps;
using Content.Shared.Mobs.Components;
using Content.Shared.Power.Components;
using Content.Shared.Teleportation.Components;
using Robust.Server.Physics;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server.Anomaly.Effects;

public sealed partial class BluespaceAnomalySystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private AtmosphereSystem _atmos = default!;
    [Dependency] private GridFixtureSystem _gridFixture = default!;
    [Dependency] private SharedDecalSystem _decal = default!;

    [Dependency] private EntityQuery<CableComponent> _cableQuery;
    [Dependency] private EntityQuery<DestructibleComponent> _destructibleQuery;
    [Dependency] private EntityQuery<ItemComponent> _itemQuery;
    [Dependency] private EntityQuery<TileEntityEffectComponent> _tileQuery;
    [Dependency] private EntityQuery<MobStateComponent> _mobStateQuery;

    private const int SwapTargetAttempts = 20;

    private readonly record struct PropMove(EntityUid Uid, Vector2 NewPos, bool Anchored);
    private readonly record struct MobMove(EntityUid Uid, Vector2 NewPos);
    private readonly record struct CableRespawn(EntityUid Uid, string ProtoId, Vector2 NewPos);
    private readonly record struct DecalMove(DecalIndex Index, Decal Decal, Vector2 NewPos);

    private sealed class ToBeMoved
    {
        public readonly List<PropMove> Props = new();
        public readonly List<MobMove> Mobs = new();
        public readonly List<CableRespawn> Cables = new();
        public readonly List<DecalMove> Decals = new();

        public int Count => Props.Count + Mobs.Count + Cables.Count;
    }


    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<BluespaceAnomalyComponent, AnomalyPulseEvent>(OnPulse);
        SubscribeLocalEvent<BluespaceAnomalyComponent, AnomalySupercriticalEvent>(OnSupercritical);
        SubscribeLocalEvent<BluespaceAnomalyComponent, AnomalySeverityChangedEvent>(OnSeverityChanged);
    }

    private void OnPulse(EntityUid uid, BluespaceAnomalyComponent component, ref AnomalyPulseEvent args)
    {
        var xform = Transform(uid);
        var range = component.MaxShuffleRadius * args.Severity * args.PowerModifier;
        // get a list of all entities in range with the MobStateComponent
        // we filter out those inside a container
        // otherwise borg brains get removed from their body, or PAIs from a PDA
        var mobs = new HashSet<Entity<MobStateComponent>>();
        _lookup.GetEntitiesInRange(xform.Coordinates, range, mobs, flags: LookupFlags.Uncontained);
        var allEnts = new ValueList<EntityUid>(mobs.Select(m => m.Owner)) { uid };
        var coords = new ValueList<Vector2>();
        foreach (var ent in allEnts)
        {
            if (TryComp(ent, out TransformComponent? allXform))
                coords.Add(_xform.GetWorldPosition(allXform));
        }

        _random.Shuffle(coords);
        for (var i = 0; i < allEnts.Count; i++)
        {
            _adminLogger.Add(LogType.Teleport, $"{ToPrettyString(allEnts[i])} has been shuffled to {coords[i]} by the {ToPrettyString(uid)} at {xform.Coordinates}");
            _xform.SetWorldPosition(allEnts[i], coords[i]);
        }
    }

    private void OnSupercritical(EntityUid uid, BluespaceAnomalyComponent component, ref AnomalySupercriticalEvent args)
    {
        if (SwapGridRegions((uid, component), ref args))
            return;

        var xform = Transform(uid);
        var mapPos = _xform.GetWorldPosition(xform);
        var radius = component.SupercriticalTeleportRadius * args.PowerModifier;
        var gridBounds = new Box2(mapPos - new Vector2(radius, radius), mapPos + new Vector2(radius, radius));
        var mobs = new HashSet<Entity<MobStateComponent>>();
        _lookup.GetEntitiesInRange(xform.Coordinates, component.MaxShuffleRadius, mobs, flags: LookupFlags.Uncontained);
        foreach (var comp in mobs)
        {
            var ent = comp.Owner;
            var randomX = _random.NextFloat(gridBounds.Left, gridBounds.Right);
            var randomY = _random.NextFloat(gridBounds.Bottom, gridBounds.Top);

            var pos = new Vector2(randomX, randomY);

            _adminLogger.Add(LogType.Teleport, $"{ToPrettyString(ent)} has been teleported to {pos} by the supercritical {ToPrettyString(uid)} at {mapPos}");

            _xform.SetWorldPosition(ent, pos);
            _audio.PlayPvs(component.TeleportSound, ent);
        }
    }

    private void OnSeverityChanged(EntityUid uid, BluespaceAnomalyComponent component, ref AnomalySeverityChangedEvent args)
    {
        if (!TryComp<PortalComponent>(uid, out var portal))
            return;
        portal.MaxRandomRadius = (component.MaxPortalRadius - component.MinPortalRadius) * args.Severity + component.MinPortalRadius;
    }

    private bool SwapGridRegions(Entity<BluespaceAnomalyComponent> ent, ref AnomalySupercriticalEvent args)
    {
        var xform = Transform(ent);
        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return false;

        var radius = ent.Comp.SupercriticalRegionSwapRadius;
        var tileA = _map.CoordinatesToTile(gridUid, grid, xform.Coordinates);

        // Find tile to swap with
        if (!TryPickSwapTarget(gridUid, grid, tileA, radius, out var tileB))
            return false;

        var sourceTiles = new List<Vector2i>();
        var destinationTiles = new List<Vector2i>();
        var tileRadius = (int)MathF.Ceiling(radius);
        var radiusSq = radius * radius;

        for (var dx = -tileRadius; dx <= tileRadius; dx++)
        {
            for (var dy = -tileRadius; dy <= tileRadius; dy++)
            {
                if (dx * dx + dy * dy > radiusSq)
                    continue;

                var offset = new Vector2i(dx, dy);

                sourceTiles.Add(tileA + offset);
                destinationTiles.Add(tileB + offset);

                sourceTiles.Add(tileB + offset);
                destinationTiles.Add(tileA + offset);
            }
        }

        // Added and removed tiles are done separately
        var fillChanges = new List<(Vector2i, Tile)>();
        var clearChanges = new List<(Vector2i, Tile)>();

        for (var i = 0; i < sourceTiles.Count; i++)
        {
            var tile = _map.GetTileRef(gridUid, grid, sourceTiles[i]).Tile;
            (tile.IsEmpty ? clearChanges : fillChanges).Add((destinationTiles[i], tile));
        }

        var gridAtmos = new Entity<GridAtmosphereComponent?, GasTileOverlayComponent?>(gridUid, null, null);
        var gasSnapshot = SnapshotGas(gridAtmos, sourceTiles);

        var moves = new ToBeMoved();
        GatherRegion(ent, gridUid, tileA, tileB, radius, moves);
        GatherRegion(ent, gridUid, tileB, tileA, radius, moves);

        _map.SetTiles(gridUid, grid, fillChanges);

        var (gridPos, gridRot) = _xform.GetWorldPositionRotation(Transform(gridUid));
        Vector2 ToWorld(Vector2 gridLocal) => gridPos + gridRot.RotateVec(gridLocal);

        var reanchor = new List<EntityUid>();

        // Cables get deleted and spawned as swapping tiles under a cable breaks them
        // Remove all cables before moving them
        foreach (var cable in moves.Cables)
        {
            Del(cable.Uid);
        }

        foreach (var cable in moves.Cables)
        {
            var spawned = SpawnAtPosition(cable.ProtoId, new EntityCoordinates(gridUid, cable.NewPos));
            Dirty(spawned, Transform(spawned));
            reanchor.Add(spawned);
        }

        foreach (var (uid, newPos, anchored) in moves.Props)
        {
            if (TerminatingOrDeleted(uid))
                continue;

            if (anchored)
            {
                _xform.Unanchor(uid);
                reanchor.Add(uid);
            }

            _xform.SetWorldPosition(uid, ToWorld(newPos));
            Dirty(uid, Transform(uid));
        }

        foreach (var (id, decal, newPos) in moves.Decals)
        {
            _decal.RemoveDecal(gridUid, id);
            _decal.TryAddDecal(decal.WithCoordinates(newPos), new EntityCoordinates(gridUid, newPos), out _);
        }

        _map.SetTiles(gridUid, grid, clearChanges);
        _gridFixture.CheckSplits(gridUid);

        foreach (var uid in reanchor)
        {
            if (!TerminatingOrDeleted(uid))
                _xform.AnchorEntity(uid);
        }

        // Mobs move last so that the states of entities are set when the player moves into pvs range
        foreach (var (uid, newPos) in moves.Mobs)
        {
            if (TerminatingOrDeleted(uid))
                continue;

            _xform.SetWorldPosition(uid, ToWorld(newPos));
            Dirty(uid, Transform(uid));
        }

        // Swaps gasses
        RestoreGas(gridAtmos, destinationTiles, gasSnapshot);

        if (TryComp<TileHistoryComponent>(gridUid, out var history))
            Dirty(gridUid, history);

        _adminLogger.Add(LogType.Teleport,
            $"{ToPrettyString(ent)} supercritical swapped grid regions at {tileA} and {tileB} ({moves.Count} entities, {sourceTiles.Count} tiles)");
        return true;
    }

    /// <summary>
    /// Finds every swappable entity in the circle around <paramref name="source"/> and records where it
    /// should end up in the circle around <paramref name="destination"/>
    /// </summary>
    private void GatherRegion(
        Entity<BluespaceAnomalyComponent> ent,
        EntityUid gridUid,
        Vector2i source,
        Vector2i destination,
        float radius,
        ToBeMoved moves)
    {
        var radiusSquare = radius * radius;
        var sourceCenter = new Vector2(source.X + 0.5f, source.Y + 0.5f);
        var delta = new Vector2(destination.X - source.X, destination.Y - source.Y);

        foreach (var found in _lookup.GetEntitiesInRange(new EntityCoordinates(gridUid, sourceCenter), radius, LookupFlags.Uncontained))
        {
            if (found == ent.Owner)
                continue;

            var foundXform = Transform(found);

            // Only things sitting directly on the grid
            if (foundXform.ParentUid != gridUid)
                continue;

            if ((foundXform.LocalPosition - sourceCenter).LengthSquared() > radiusSquare)
                continue;

            var isMob = _mobStateQuery.HasComp(found);
            if (!isMob && !_destructibleQuery.HasComp(found) && !_itemQuery.HasComp(found) && !_tileQuery.HasComp(found))
                continue;

            var newPos = foundXform.LocalPosition + delta;

            if (isMob)
            {
                moves.Mobs.Add(new MobMove(found, newPos));
            }
            else if (foundXform.Anchored && _cableQuery.HasComp(found) && MetaData(found).EntityPrototype is { } proto)
            {
                moves.Cables.Add(new CableRespawn(found, proto.ID, newPos));
            }
            else
            {
                moves.Props.Add(new PropMove(found, newPos, foundXform.Anchored));
            }
        }

        foreach (var (index, decal) in _decal.GetDecalsInRange(gridUid, sourceCenter, radius))
        {
            if ((decal.Coordinates - sourceCenter).LengthSquared() > radiusSquare)
                continue;
            var newPos = decal.Coordinates + delta;
            moves.Decals.Add(new DecalMove(index, decal, newPos));
        }
    }

    private GasMixture?[] SnapshotGas(Entity<GridAtmosphereComponent?, GasTileOverlayComponent?> gridAtmos, List<Vector2i> tiles)
    {
        var snapshot = new GasMixture?[tiles.Count];
        var live = _atmos.GetTileMixtures(gridAtmos, null, tiles);
        if (live == null)
            return snapshot;

        for (var i = 0; i < snapshot.Length; i++)
        {
            snapshot[i] = live[i]?.Clone();
        }

        return snapshot;
    }

    private void RestoreGas(Entity<GridAtmosphereComponent?, GasTileOverlayComponent?> gridAtmos, List<Vector2i> tiles, GasMixture?[] snapshot)
    {
        var live = _atmos.GetTileMixtures(gridAtmos, null, tiles, excite: true);
        if (live == null)
            return;

        for (var i = 0; i < live.Length; i++)
        {
            if (snapshot[i] is not { } gas || live[i] is not { } dst)
                continue;

            // GetTileMixtures hands back the shared space mixture for tiles with no atmos; never write to that.
            if (dst.Immutable)
                continue;

            dst.CopyFrom(gas);
        }
    }

    /// <summary>
    /// Picks a random tile on the grid whose circle doesn't overlap the circle around <paramref name="source"/>.
    /// </summary>
    private bool TryPickSwapTarget(EntityUid gridUid, MapGridComponent grid, Vector2i source, float radius, out Vector2i target)
    {
        var bounds = grid.LocalAABB;
        var minDistSq = 8f * radius * radius;

        for (var i = 0; i < SwapTargetAttempts; i++)
        {
            var candidate = new Vector2i(
                (int)MathF.Floor(_random.NextFloat(bounds.Left, bounds.Right)),
                (int)MathF.Floor(_random.NextFloat(bounds.Bottom, bounds.Top)));

            var dx = candidate.X - source.X;
            var dy = candidate.Y - source.Y;

            if (dx * dx + dy * dy <= minDistSq)
                continue;

            if (_map.GetTileRef(gridUid, grid, candidate).Tile.IsEmpty)
                continue;

            target = candidate;
            return true;
        }

        target = default;
        return false;
    }
}
