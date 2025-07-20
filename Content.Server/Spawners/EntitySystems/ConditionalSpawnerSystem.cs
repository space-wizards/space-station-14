using System.Numerics;
using Content.Server.GameTicking;
using Content.Server.Spawners.Components;
using Content.Shared.EntityTable;
using Content.Shared.GameTicking.Components;
using Content.Shared.Stacks;
using JetBrains.Annotations;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Spawners.EntitySystems;

[UsedImplicitly]
public sealed class ConditionalSpawnerSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly EntityTableSystem _entityTable = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GameRuleStartedEvent>(OnRuleStarted);
        SubscribeLocalEvent<ConditionalSpawnerComponent, MapInitEvent>(OnCondSpawnMapInit);
        SubscribeLocalEvent<RandomSpawnerComponent, MapInitEvent>(OnRandSpawnMapInit);
        SubscribeLocalEvent<EntityTableSpawnerComponent, MapInitEvent>(OnEntityTableSpawnMapInit);
    }

    private void OnCondSpawnMapInit(EntityUid uid, ConditionalSpawnerComponent component, MapInitEvent args)
    {
        TrySpawn(uid, component);
    }

    private void OnRandSpawnMapInit(EntityUid uid, RandomSpawnerComponent component, MapInitEvent args)
    {
        Spawn(uid, component);
        if (component.DeleteSpawnerAfterSpawn)
            QueueDel(uid);
    }

    private void OnEntityTableSpawnMapInit(Entity<EntityTableSpawnerComponent> ent, ref MapInitEvent args)
    {
        Spawn(ent);
        if (ent.Comp.DeleteSpawnerAfterSpawn && !TerminatingOrDeleted(ent) && Exists(ent))
            QueueDel(ent);
    }

    private void OnRuleStarted(ref GameRuleStartedEvent args)
    {
        var query = EntityQueryEnumerator<ConditionalSpawnerComponent>();
        while (query.MoveNext(out var uid, out var spawner))
        {
            RuleStarted(uid, spawner, args);
        }
    }

    public void RuleStarted(EntityUid uid, ConditionalSpawnerComponent component, GameRuleStartedEvent obj)
    {
        if (component.GameRules.Contains(obj.RuleId))
            Spawn(uid, component);
    }

    private void TrySpawn(EntityUid uid, ConditionalSpawnerComponent component)
    {
        if (component.GameRules.Count == 0)
        {
            Spawn(uid, component);
            return;
        }

        foreach (var rule in component.GameRules)
        {
            if (!_ticker.IsGameRuleActive(rule))
                continue;
            Spawn(uid, component);
            return;
        }
    }

    private void Spawn(EntityUid uid, ConditionalSpawnerComponent component)
    {
        if (component.Chance != 1.0f && !_robustRandom.Prob(component.Chance))
            return;

        if (component.Prototypes.Count == 0)
        {
            Log.Warning($"Prototype list in ConditionalSpawnComponent is empty! Entity: {ToPrettyString(uid)}");
            return;
        }

        if (!Deleted(uid))
            Spawn(_robustRandom.Pick(component.Prototypes), Transform(uid).Coordinates);
    }

    private void Spawn(EntityUid uid, RandomSpawnerComponent component)
    {
        if (component.RarePrototypes.Count > 0 && (component.RareChance == 1.0f || _robustRandom.Prob(component.RareChance)))
        {
            Spawn(_robustRandom.Pick(component.RarePrototypes), Transform(uid).Coordinates);
            return;
        }

        if (component.Chance != 1.0f && !_robustRandom.Prob(component.Chance))
            return;

        if (component.Prototypes.Count == 0)
        {
            Log.Warning($"Prototype list in RandomSpawnerComponent is empty! Entity: {ToPrettyString(uid)}");
            return;
        }

        if (Deleted(uid))
            return;

        var offset = component.Offset;
        var xOffset = _robustRandom.NextFloat(-offset, offset);
        var yOffset = _robustRandom.NextFloat(-offset, offset);

        var coordinates = Transform(uid).Coordinates.Offset(new Vector2(xOffset, yOffset));

        Spawn(_robustRandom.Pick(component.Prototypes), coordinates);
    }

    private void Spawn(Entity<EntityTableSpawnerComponent> ent)
    {
        if (TerminatingOrDeleted(ent) || !Exists(ent))
            return;

        var coords = Transform(ent).Coordinates;

        EntityTableSpawnerComponent comp = ent;
        var spawns = _entityTable.GetSpawns(comp.Table);
        if (comp.AutoStack)
        {
            SpawnStackedWhenPossible(spawns, coords, comp.Offset);
        }
        else
        {
            SpawnAtRandomOffset(spawns, coords, comp.Offset);
        }
    }

    private void SpawnStackedWhenPossible(
        IEnumerable<EntProtoId> spawns,
        EntityCoordinates coords,
        float offset
    )
    {
        Dictionary<ProtoId<StackPrototype>, (EntProtoId Proto, int Count, int? StackMaxCount)> prototypeStacks = new();
        ValueList<EntProtoId> nonStackable = [];
        foreach (var protoId in spawns)
        {
            var prototype = _prototypeManager.Index(protoId);
            if (!Factory.TryGetComponent<StackComponent>(prototype.Components, out var stack))
            {
                nonStackable.Add(protoId);
                continue;
            }

            if (prototypeStacks.TryGetValue(stack.StackTypeId, out var found))
            {
                prototypeStacks[stack.StackTypeId] = (protoId, found.Count + 1, found.StackMaxCount);
            }
            else
            {
                var stackPrototype = _prototypeManager.Index(stack.StackTypeId);
                prototypeStacks[stack.StackTypeId] = (protoId, 1, stackPrototype.MaxCount);
            }
        }

        SpawnAtRandomOffset(nonStackable, coords, offset);

        foreach (var (protoId, count, maxSize) in prototypeStacks.Values)
        {
            if (!maxSize.HasValue)
            {
                var spawnAllInOne = SpawnAtRandomOffset(protoId, coords, offset);
                if (!TryComp<StackComponent>(spawnAllInOne, out var allInOneStackComp))
                    continue;

                allInOneStackComp.Count = count;
                Dirty(spawnAllInOne, allInOneStackComp);
                continue;
            }

            var repeatCount = count / maxSize.Value;
            var leftOver = count % maxSize.Value;
            for (int i = 0; i < repeatCount; i++)
            {
                var spawned = SpawnAtRandomOffset(protoId, coords, offset);
                if (!TryComp<StackComponent>(spawned, out var stackComp))
                    continue;

                stackComp.Count = maxSize.Value;
                Dirty(spawned, stackComp);
            }

            var spawnedForLeftOver = SpawnAtRandomOffset(protoId, coords, offset);
            if (!TryComp<StackComponent>(spawnedForLeftOver, out var stackCompOfLeftOver))
                continue;

            stackCompOfLeftOver.Count = leftOver;
            Dirty(spawnedForLeftOver, stackCompOfLeftOver);
        }
    }

    private void SpawnAtRandomOffset(IEnumerable<EntProtoId> spawns, EntityCoordinates coords, float offset)
    {
        foreach (var proto in spawns)
        {
            SpawnAtRandomOffset(proto, coords, offset);
        }
    }

    private EntityUid SpawnAtRandomOffset(EntProtoId proto, EntityCoordinates coords, float offset)
    {
        var xOffset = _robustRandom.NextFloat(-offset, offset);
        var yOffset = _robustRandom.NextFloat(-offset, offset);
        var trueCoords = coords.Offset(new Vector2(xOffset, yOffset));

        return SpawnAtPosition(proto, trueCoords);
    }
}
