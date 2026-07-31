// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System.Linq;
using System.Numerics;
using Content.Server.Explosion.EntitySystems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.DeadSpace.GameRules.Components;
using Content.Shared.EntityTable;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.GameTicking.Components;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Tag;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server.DeadSpace.GameRules;

public sealed class BrokenTechGameRuleSystem : GameRuleSystem<BrokenTechGameRuleComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityTableSystem _entityTable = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly TagSystem _tags = default!;
    [Dependency] private readonly ArrivalsSystem _arrivals = default!;
    [Dependency] private readonly StationSystem _station = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (GameTicker.RunLevel == GameRunLevel.PostRound)
            return;

        var query = EntityQueryEnumerator<BrokenTechGameRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var ruleComp, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(uid, gameRule))
                continue;

            foreach (var entry in ruleComp.ListComponent)
            {
                entry.ElapsedSeconds += frameTime;

                if (entry.ElapsedSeconds < entry.NextAttemptSeconds)
                    continue;

                if (_random.Next(100) >= entry.Chance)
                {
                    var maxSeconds = entry.MinuteMax * 60f;
                    var remaining = maxSeconds - entry.ElapsedSeconds;

                    entry.NextAttemptSeconds = entry.ElapsedSeconds +
                        (remaining > 5f ? _random.NextFloat(1f, MathF.Min(remaining, 30f)) : 5f);
                    continue;
                }

                ExecuteEntry(entry);

                entry.ElapsedSeconds = 0f;
                var minSec = entry.MinuteMin * 60f;
                var maxSec = entry.MinuteMax * 60f;
                entry.NextAttemptSeconds = _random.NextFloat(minSec, maxSec);
            }
        }
    }

    protected override void Started(EntityUid uid, BrokenTechGameRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        foreach (var entry in component.ListComponent)
        {
            entry.ElapsedSeconds = 0f;
            entry.Triggered = false;
            var minSeconds = entry.MinuteMin * 60f;
            var maxSeconds = entry.MinuteMax * 60f;
            entry.NextAttemptSeconds = _random.NextFloat(minSeconds, maxSeconds);
        }
    }

    private void ExecuteEntry(BrokenTechEntry entry)
    {
        var entities = GetEntitiesWithComponent(entry.ComponentName);
        if (entities.Count == 0)
            return;

        _random.Shuffle(entities);

        var filtered = FilterEntities(entities, entry);

        var targets = filtered
            .Take(entry.HowMuchEntity)
            .ToList();

        foreach (var target in targets)
        {
            switch (entry.Action)
            {
                case ExplodeEntityAction explode:
                    HandleExplode(target, explode);
                    break;
                case BlockWorkingEntityAction block:
                    HandleBlock(target, block);
                    break;
            }
        }
    }

    private List<EntityUid> FilterEntities(List<EntityUid> entities, BrokenTechEntry entry)
    {
        var result = new List<EntityUid>();

        foreach (var ent in entities)
        {
            if (TerminatingOrDeleted(ent))
                continue;

            if (!IsValidEventTarget(ent))
                continue;

            var meta = MetaData(ent);
            if (meta.EntityPrototype != null
                && entry.BlacklistPrototypes.Contains(meta.EntityPrototype.ID))
            {
                continue;
            }

            var hasBlacklistedTag = false;
            foreach (var tagId in entry.BlacklistTags)
            {
                if (_tags.HasTag(ent, tagId))
                {
                    hasBlacklistedTag = true;
                    break;
                }
            }
            if (hasBlacklistedTag)
                continue;

            result.Add(ent);
        }

        return result;
    }

    private bool IsValidEventTarget(EntityUid ent)
    {
        if (!TryComp(ent, out TransformComponent? xform))
            return false;

        if (_arrivals.IsOnArrivals((ent, xform)))
            return false;

        var station = _station.GetOwningStation(ent, xform);
        return station != null && HasComp<StationEventEligibleComponent>(station.Value);
    }

    private List<EntityUid> GetEntitiesWithComponent(string componentName)
    {
        var result = new List<EntityUid>();
        if (!_compFactory.TryGetRegistration(componentName, out var registration))
            return result;

        foreach (var comp in EntityManager.GetAllComponents(registration.Type))
        {
            result.Add(comp.Uid);
        }

        return result;
    }

    private void HandleExplode(EntityUid uid, ExplodeEntityAction action)
    {
        if (TerminatingOrDeleted(uid))
            return;

        SpawnFromTable(uid, action.SpawnTable);
        _explosion.QueueExplosion(uid, action.ExplosionType, action.ExplosionIntensity, 1f, 2f, maxTileBreak: 0);
        QueueDel(uid);
    }

    private void HandleBlock(EntityUid uid, BlockWorkingEntityAction action)
    {
        if (Deleted(uid))
            return;

        SpawnFromTable(uid, action.SpawnTable);

        if (TryComp<ApcComponent>(uid, out var apc))
        {
            apc.MainBreakerEnabled = false;
            Dirty(uid, apc);
            EnsureComp<BrokenTechPowerDisabledComponent>(uid);
            return;
        }

        if (TryComp<ApcPowerReceiverComponent>(uid, out var receiver))
        {
            receiver.PowerDisabled = true;
            Dirty(uid, receiver);
            EnsureComp<BrokenTechPowerDisabledComponent>(uid);
            return;
        }

        if (_compFactory.TryGetRegistration("NodeContainer", out var nodeReg)
            && HasComp(uid, nodeReg.Type))
        {
            EnsureComp<BrokenTechPowerDisabledComponent>(uid);
            RemComp(uid, nodeReg.Type);
            return;
        }

        QueueDel(uid);
    }

    private void SpawnFromTable(EntityUid anchor, EntityTableSelector? selector)
    {
        if (selector == null)
            return;

        var spawns = _entityTable.GetSpawns(selector).ToList();
        if (spawns.Count == 0)
            return;

        var xform = Transform(anchor);
        var spawnPos = FindAtmosphericNeighbor(anchor, xform.GridUid);
        if (spawnPos == null)
            return;

        foreach (var proto in spawns)
        {
            Spawn(proto, new MapCoordinates(spawnPos.Value, xform.MapID));
        }
    }

    private Vector2? FindAtmosphericNeighbor(EntityUid anchor, EntityUid? gridUid)
    {
        if (gridUid is not { } gridEnt || !TryComp<MapGridComponent>(gridEnt, out var grid))
            return null;

        var worldPos = _transform.GetWorldPosition(anchor);
        var tilePos = _mapSystem.WorldToTile(gridEnt, grid, worldPos);
        var mapUid = Transform(gridEnt).MapUid;

        var neighbors = new[]
        {
            tilePos + new Vector2i(0, -1),
            tilePos + new Vector2i(0, 1),
            tilePos + new Vector2i(1, 0),
            tilePos + new Vector2i(-1, 0),
        };

        foreach (var neighbor in neighbors)
        {
            var tile = _mapSystem.GetTileRef(gridEnt, grid, neighbor);
            if (tile.Tile.IsEmpty)
                continue;

            var mixture = _atmos.GetTileMixture(gridEnt, mapUid, neighbor);
            if (mixture != null && mixture.TotalMoles > 0)
                return _mapSystem.GridTileToWorldPos(gridEnt, grid, neighbor);
        }

        return null;
    }

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BrokenTechPowerDisabledComponent, SignalReceivedEvent>(OnSignalRestorePower);
    }

    private void OnSignalRestorePower(EntityUid uid, BrokenTechPowerDisabledComponent comp, ref SignalReceivedEvent args)
    {
        if (TryComp<ApcComponent>(uid, out var apc))
        {
            apc.MainBreakerEnabled = true;
            Dirty(uid, apc);
            RemComp<BrokenTechPowerDisabledComponent>(uid);
            return;
        }

        if (!TryComp<ApcPowerReceiverComponent>(uid, out var receiver))
            return;

        receiver.PowerDisabled = false;
        Dirty(uid, receiver);
        RemComp<BrokenTechPowerDisabledComponent>(uid);
    }
}
