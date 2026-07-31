// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System;
using System.Collections.Generic;
using Content.Server.Atmos.Components;
using Content.Server.GameTicking;
using Content.Shared.Atmos;
using Content.Shared.Backmen.Blob.Components;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage.Systems;
using Content.Shared.DeadSpace.GameRules.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Content.Shared.Item;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Server.DeadSpace.GameRules;

public sealed class BrokenTechFireSpreadSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;

    private const int FireProcessBudget = 64;
    private static readonly TimeSpan WaterCheckInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan DormantCheckInterval = TimeSpan.FromSeconds(5);

    private readonly PriorityQueue<FireScheduleEntry, TimeSpan> _fireQueue = new();
    private readonly Dictionary<EntityUid, int> _scheduleGenerations = new();
    private readonly Dictionary<EntityUid, FireTileKey> _fireTilesByUid = new();
    private readonly Dictionary<FireTileKey, int> _fireTileCounts = new();
    private readonly HashSet<Entity<SolutionContainerManagerComponent>> _solutionTileEntities = new();

    private EntityQuery<AirtightComponent> _airtightQuery;
    private EntityQuery<PuddleComponent> _puddleQuery;
    private EntityQuery<ItemComponent> _itemQuery;
    private EntityQuery<TransformComponent> _xformQuery;
    private EntityQuery<BlobTileComponent> _blobTileQuery;

    private static readonly AtmosDirection[] Directions =
    {
        AtmosDirection.North,
        AtmosDirection.East,
        AtmosDirection.South,
        AtmosDirection.West,
    };

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_gameTicker.RunLevel == GameRunLevel.PostRound)
            return;

        var curTime = _timing.CurTime;
        var processed = 0;
        var dequeued = 0;
        var maxDequeues = FireProcessBudget * 4;

        while (processed < FireProcessBudget &&
               dequeued < maxDequeues &&
               _fireQueue.TryPeek(out var entry, out var dueTime) &&
               dueTime <= curTime)
        {
            _fireQueue.Dequeue();
            dequeued++;

            if (!_scheduleGenerations.TryGetValue(entry.Uid, out var generation) ||
                generation != entry.Generation ||
                TerminatingOrDeleted(entry.Uid) ||
                !TryComp<BrokenTechFireSpreadComponent>(entry.Uid, out var fire))
            {
                continue;
            }

            processed++;
            ProcessFire(entry.Uid, fire);
        }
    }

    public override void Initialize()
    {
        base.Initialize();

        _airtightQuery = GetEntityQuery<AirtightComponent>();
        _puddleQuery = GetEntityQuery<PuddleComponent>();
        _itemQuery = GetEntityQuery<ItemComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();
        _blobTileQuery = GetEntityQuery<BlobTileComponent>();

        SubscribeLocalEvent<BrokenTechFireSpreadComponent, ComponentStartup>(OnFireStartup);
        SubscribeLocalEvent<BrokenTechFireSpreadComponent, ComponentShutdown>(OnFireShutdown);
        SubscribeLocalEvent<BrokenTechFireSpreadComponent, ReactionEntityEvent>(OnReaction);
    }

    private void OnFireStartup(Entity<BrokenTechFireSpreadComponent> ent, ref ComponentStartup args)
    {
        if (_xformQuery.TryGetComponent(ent.Owner, out var xform) &&
            xform.GridUid is { } gridUid &&
            TryComp<MapGridComponent>(gridUid, out var grid))
        {
            var tile = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);
            UpdateTileIndex(ent.Owner, gridUid, tile);
            InitializeOrigin(ent.Comp, gridUid, tile);
        }

        ScheduleFire(ent.Owner, _timing.CurTime);
    }

    private void OnFireShutdown(Entity<BrokenTechFireSpreadComponent> ent, ref ComponentShutdown args)
    {
        _scheduleGenerations.Remove(ent.Owner);
        RemoveTileIndex(ent.Owner);
    }

    private void ScheduleNext(EntityUid uid, BrokenTechFireSpreadComponent fire)
    {
        var curTime = _timing.CurTime;
        if (fire.Finished)
        {
            var nextFinished = fire.NextWaterCheck;

            if (!fire.BlobTileDamage.Empty && fire.NextBlobTileDamage < nextFinished)
                nextFinished = fire.NextBlobTileDamage;

            if (nextFinished < curTime)
                nextFinished = curTime + DormantCheckInterval;

            ScheduleFire(uid, nextFinished);
            return;
        }

        var next = fire.NextWaterCheck;

        if (!fire.BlobTileDamage.Empty && fire.NextBlobTileDamage < next)
            next = fire.NextBlobTileDamage;

        if (fire.Distance < fire.MaxRadius && fire.NextSpread < next)
            next = fire.NextSpread;

        if (next < curTime)
            next = curTime;

        ScheduleFire(uid, next);
    }

    private void ScheduleFire(EntityUid uid, TimeSpan when)
    {
        if (TerminatingOrDeleted(uid))
            return;

        var generation = _scheduleGenerations.GetValueOrDefault(uid) + 1;
        _scheduleGenerations[uid] = generation;
        _fireQueue.Enqueue(new FireScheduleEntry(uid, generation), when);
    }

    private void UpdateTileIndex(EntityUid uid, EntityUid gridUid, Vector2i tile)
    {
        var key = new FireTileKey(gridUid, tile);
        if (_fireTilesByUid.TryGetValue(uid, out var oldKey) && oldKey == key)
            return;

        RemoveTileIndex(uid);
        _fireTilesByUid[uid] = key;
        _fireTileCounts[key] = _fireTileCounts.GetValueOrDefault(key) + 1;
    }

    private void RemoveTileIndex(EntityUid uid)
    {
        if (!_fireTilesByUid.Remove(uid, out var key))
            return;

        var count = _fireTileCounts[key] - 1;
        if (count <= 0)
            _fireTileCounts.Remove(key);
        else
            _fireTileCounts[key] = count;
    }

    private void ProcessFire(EntityUid uid, BrokenTechFireSpreadComponent fire)
    {
        if (!_xformQuery.TryGetComponent(uid, out var xform) ||
            xform.GridUid is not { } gridUid ||
            !TryComp<MapGridComponent>(gridUid, out var grid))
        {
            RemoveTileIndex(uid);
            fire.Finished = true;
            ScheduleFire(uid, _timing.CurTime + DormantCheckInterval);
            return;
        }

        var curTime = _timing.CurTime;
        var tile = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);
        UpdateTileIndex(uid, gridUid, tile);
        InitializeOrigin(fire, gridUid, tile);

        if (fire.Finished)
        {
            if (curTime >= fire.NextWaterCheck)
            {
                if (IsWaterTile(gridUid, grid, tile, fire))
                {
                    QueueDel(uid);
                    return;
                }

                fire.NextWaterCheck = curTime + DormantCheckInterval;
            }

            DamageBlobTiles(fire, gridUid, grid, tile);
            ScheduleNext(uid, fire);
            return;
        }

        if (curTime >= fire.NextWaterCheck)
        {
            if (IsWaterTile(gridUid, grid, tile, fire))
            {
                QueueDel(uid);
                return;
            }

            fire.NextWaterCheck = curTime + WaterCheckInterval;
        }

        DamageBlobTiles(fire, gridUid, grid, tile);

        if (fire.Distance >= fire.MaxRadius)
        {
            fire.Finished = true;
            ScheduleFire(uid, curTime + DormantCheckInterval);
            return;
        }

        if (curTime >= fire.NextSpread)
        {
            fire.NextSpread = curTime + TimeSpan.FromSeconds(fire.SpreadDelay);

            if (!TrySpread(uid, fire, gridUid, grid, tile))
                fire.Finished = true;
        }

        ScheduleNext(uid, fire);
    }

    private void OnReaction(Entity<BrokenTechFireSpreadComponent> ent, ref ReactionEntityEvent args)
    {
        if (args.Method != ReactionMethod.Touch)
            return;

        if (!HasWaterReagent(args.Reagent.ID, ent.Comp))
            return;

        QueueDel(ent);
    }

    private void InitializeOrigin(BrokenTechFireSpreadComponent fire, EntityUid gridUid, Vector2i tile)
    {
        if (fire.HasOrigin)
            return;

        fire.OriginGrid = gridUid;
        fire.OriginTile = tile;
        fire.HasOrigin = true;
        fire.Distance = 0;
        fire.NextSpread = _timing.CurTime + TimeSpan.FromSeconds(fire.SpreadDelay);
    }

    private bool TrySpread(
        EntityUid uid,
        BrokenTechFireSpreadComponent fire,
        EntityUid gridUid,
        MapGridComponent grid,
        Vector2i tile)
    {
        var prototype = MetaData(uid).EntityPrototype?.ID;
        if (prototype == null || fire.OriginGrid != gridUid)
            return false;

        var spawned = false;
        foreach (var direction in Directions)
        {
            var neighborTile = tile.Offset(direction);
            var opposite = direction.GetOpposite();

            if (!_map.TryGetTileRef(gridUid, grid, neighborTile, out var tileRef) || tileRef.Tile.IsEmpty)
                continue;

            if (IsBlockedByAirtight(gridUid, grid, tile, direction) ||
                IsBlockedByAirtight(gridUid, grid, neighborTile, opposite) ||
                IsWaterTile(gridUid, grid, neighborTile, fire) ||
                HasBrokenTechFireAt(gridUid, grid, neighborTile))
            {
                continue;
            }

            var child = Spawn(prototype, _map.GridTileToLocal(gridUid, grid, neighborTile));
            if (!TryComp<BrokenTechFireSpreadComponent>(child, out var childFire))
                continue;

            childFire.OriginGrid = fire.OriginGrid;
            childFire.OriginTile = fire.OriginTile;
            childFire.HasOrigin = true;
            childFire.Distance = fire.Distance + 1;
            childFire.MaxRadius = fire.MaxRadius;
            childFire.SpreadDelay = fire.SpreadDelay;
            childFire.WaterReagents = new(fire.WaterReagents);
            childFire.BlobTileDamage = new(fire.BlobTileDamage);
            childFire.BlobTileDamageInterval = fire.BlobTileDamageInterval;
            childFire.NextSpread = _timing.CurTime + TimeSpan.FromSeconds(childFire.SpreadDelay);
            UpdateTileIndex(child, gridUid, neighborTile);
            ScheduleNext(child, childFire);
            spawned = true;
        }

        return spawned;
    }

    private bool IsBlockedByAirtight(EntityUid gridUid, MapGridComponent grid, Vector2i tile, AtmosDirection direction)
    {
        var anchored = _map.GetAnchoredEntitiesEnumerator(gridUid, grid, tile);

        while (anchored.MoveNext(out var ent))
        {
            if (!_airtightQuery.TryGetComponent(ent, out var airtight) || !airtight.AirBlocked)
                continue;

            if ((airtight.AirBlockedDirection & direction) != 0x0)
                return true;
        }

        return false;
    }

    private bool IsWaterTile(
        EntityUid gridUid,
        MapGridComponent grid,
        Vector2i tile,
        BrokenTechFireSpreadComponent fire)
    {
        var anchored = _map.GetAnchoredEntitiesEnumerator(gridUid, grid, tile);

        while (anchored.MoveNext(out var ent))
        {
            if (!_puddleQuery.TryGetComponent(ent, out var puddle) ||
                !_solutionContainer.ResolveSolution(ent.Value, puddle.SolutionName, ref puddle.Solution, out var solution))
            {
                continue;
            }

            if (HasWaterReagent(solution, fire))
                return true;
        }

        _solutionTileEntities.Clear();
        _lookup.GetLocalEntitiesIntersecting(gridUid, tile, _solutionTileEntities, flags: LookupFlags.Uncontained, gridComp: grid);

        foreach (var ent in _solutionTileEntities)
        {
            if (!IsWaterBlockingEntity(ent.Owner))
                continue;

            foreach (var (_, solutionEntity) in _solutionContainer.EnumerateSolutions((ent.Owner, ent.Comp)))
            {
                if (HasWaterReagent(solutionEntity.Comp.Solution, fire))
                {
                    _solutionTileEntities.Clear();
                    return true;
                }
            }
        }

        _solutionTileEntities.Clear();
        return false;
    }

    private bool IsWaterBlockingEntity(EntityUid uid)
    {
        if (_itemQuery.HasComponent(uid))
            return false;

        return _xformQuery.TryGetComponent(uid, out var xform) && xform.Anchored;
    }

    private bool HasWaterReagent(Solution solution, BrokenTechFireSpreadComponent fire)
    {
        foreach (var reagent in fire.WaterReagents)
        {
            if (solution.GetTotalPrototypeQuantity(reagent) > FixedPoint2.Zero)
                return true;
        }

        return false;
    }

    private bool HasWaterReagent(string reagentId, BrokenTechFireSpreadComponent fire)
    {
        foreach (var reagent in fire.WaterReagents)
        {
            if (reagent == reagentId)
                return true;
        }

        return false;
    }

    private bool HasBrokenTechFireAt(EntityUid gridUid, MapGridComponent grid, Vector2i tile)
    {
        return _fireTileCounts.ContainsKey(new FireTileKey(gridUid, tile));
    }

    private void DamageBlobTiles(
        BrokenTechFireSpreadComponent fire,
        EntityUid gridUid,
        MapGridComponent grid,
        Vector2i tile)
    {
        if (fire.BlobTileDamage.Empty || _timing.CurTime < fire.NextBlobTileDamage)
            return;

        fire.NextBlobTileDamage = _timing.CurTime + TimeSpan.FromSeconds(Math.Max(fire.BlobTileDamageInterval, 0.1f));

        var anchored = _map.GetAnchoredEntitiesEnumerator(gridUid, grid, tile);

        while (anchored.MoveNext(out var ent))
        {
            if (!_blobTileQuery.HasComponent(ent))
                continue;

            _damageable.TryChangeDamage(ent.Value, fire.BlobTileDamage, interruptsDoAfters: false);
        }
    }
    private readonly record struct FireScheduleEntry(
        EntityUid Uid,
        int Generation);

    private readonly record struct FireTileKey(
        EntityUid GridUid,
        Vector2i Tile);
}
