using Content.Server.GameTicking;
using Content.Server.Spawners.Components;
using Content.Server.Stack;
using Content.Shared.EntityTable;
using Content.Shared.GameTicking.Components;
using Content.Shared.Stacks;
using Robust.Server.GameObjects;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Spawners.EntitySystems;

// TODO: This whole system is a mess. A lot of this should be marked obsolete.
// TODO: It should probably use interfaces with entity tables *if* more than one component is needed.
// TODO: Remove the TransformSystem Dependency when engine SpawnAtPosition EntityCoordinates override is fixed.
public sealed partial class ConditionalSpawnerSystem : EntitySystem
{
    [Dependency] private IRobustRandom _robustRandom = default!;
    [Dependency] private GameTicker _ticker = default!;
    [Dependency] private EntityTableSystem _entityTable = default!;
    [Dependency] private StackSystem _stack = default!;
    [Dependency] private TransformSystem _xform = default!;

    [SubscribeLocalEvent]
    private void OnCondSpawnMapInit(Entity<ConditionalSpawnerComponent> ent, ref MapInitEvent args)
    {
        TrySpawn(ent, ent);
    }

    [SubscribeLocalEvent]
    private void OnRandSpawnMapInit(Entity<RandomSpawnerComponent> ent, ref MapInitEvent args)
    {
        Spawn(ent, ent);
        if (ent.Comp.DeleteSpawnerAfterSpawn)
            QueueDel(ent);
    }

    [SubscribeLocalEvent]
    private void OnEntityTableSpawnMapInit(Entity<EntityTableSpawnerComponent> ent, ref MapInitEvent args)
    {
        Spawn(ent);
        if (ent.Comp.DeleteSpawnerAfterSpawn && !TerminatingOrDeleted(ent) && Exists(ent))
            QueueDel(ent);
    }

    [SubscribeLocalEvent]
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

        if (Deleted(uid))
            return;

        var xform = Transform(uid);
        var coords = _xform.GetMapCoordinates(uid, xform);
        var rotation = _xform.GetWorldRotation(xform);

        var toSpawn = _robustRandom.Pick(component.Prototypes);
        Spawn(toSpawn, coords, rotation: rotation);
    }

    private void Spawn(EntityUid uid, RandomSpawnerComponent component)
    {
        if (Deleted(uid))
            return;

        if (GetPrototype((uid, component)) is not { } proto)
            return;

        var xform = Transform(uid);
        var coords = _xform.GetMapCoordinates(uid, xform);
        var coordinates = GetRandomOffset(coords, component.Offset);
        var rotation = _xform.GetWorldRotation(xform);

        Spawn(_robustRandom.Pick(component.Prototypes), coordinates, rotation: rotation);
    }

    private void Spawn(Entity<EntityTableSpawnerComponent> ent)
    {
        if (TerminatingOrDeleted(ent) || !Exists(ent))
            return;

        var xform = Transform(ent);
        var coords = _xform.GetMapCoordinates(ent, xform);
        var rotation = _xform.GetWorldRotation(xform);

        EntityTableSpawnerComponent comp = ent;
        var spawns = _entityTable.GetSpawns(comp.Table);
        if (comp.AutoStack)
        {
            SpawnStackedWhenPossible(spawns, ent, coords, comp.Offset, rotation);
        }
        else
        {
            SpawnAtRandomOffset(spawns, coords, comp.Offset, rotation);
        }
    }

    private void SpawnStackedWhenPossible(IEnumerable<EntProtoId> spawns,
        Entity<EntityTableSpawnerComponent> ent,
        MapCoordinates coords,
        float offset,
        Angle rotation)
    {
        Dictionary<ProtoId<StackPrototype>, (EntProtoId Proto, int Count)> prototypeStacks = new();
        ValueList<EntProtoId> nonStackable = [];
        foreach (var protoId in spawns)
        {
            var prototype = ProtoMan.Index(protoId);
            if (!prototype.TryComp<StackComponent>(out var stack, Factory))
            {
                nonStackable.Add(protoId);
                continue;
            }

            prototypeStacks[stack.StackTypeId] = prototypeStacks.TryGetValue(stack.StackTypeId, out var found)
                ? (protoId, found.Count + 1)
                : (protoId, 1);
        }

        SpawnAtRandomOffset(nonStackable, coords, offset, rotation);

        foreach (var (protoId, count) in prototypeStacks.Values)
        {
            var trueCoords = GetRandomOffset(coords, offset);
            var entCoordinates = _xform.ToCoordinates((ent, null), trueCoords);
            _stack.SpawnMultipleAtPosition(protoId, count, entCoordinates);
        }
    }

    private void SpawnAtRandomOffset(IEnumerable<EntProtoId> spawns, MapCoordinates coords, float offset, Angle rotation)
    {
        foreach (var proto in spawns)
        {
            SpawnAtRandomOffset(proto, coords, offset, rotation);
        }
    }

    private EntityUid SpawnAtRandomOffset(EntProtoId proto, MapCoordinates coords, float offset, Angle rotation)
    {
        var trueCoords = GetRandomOffset(coords, offset);

        return Spawn(proto, trueCoords, rotation: rotation);
    }

    private EntProtoId? GetPrototype(Entity<RandomSpawnerComponent> spawner)
    {
        if (GetPrototypes(spawner) is not { } list)
            return null;

        return _robustRandom.Pick(list);
    }

    private List<EntProtoId>? GetPrototypes(Entity<RandomSpawnerComponent> spawner)
    {
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (spawner.Comp.RarePrototypes.Count > 0 &&
            (spawner.Comp.RareChance == 1.0f || _robustRandom.Prob(spawner.Comp.RareChance)))
        {
            return spawner.Comp.RarePrototypes;
        }

        if (spawner.Comp.Prototypes.Count == 0)
        {
            Log.Warning($"Prototype list in RandomSpawnerComponent is empty! Entity: {ToPrettyString(spawner)}");
            return null;
        }

        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (spawner.Comp.Chance == 1.0f || _robustRandom.Prob(spawner.Comp.Chance))
        {
            return spawner.Comp.Prototypes;
        }

        return null;
    }

    private MapCoordinates GetRandomOffset(MapCoordinates coords, float offset)
    {
        var vOffset = _robustRandom.NextVector2Box(offset, offset);
        return coords.Offset(vOffset);
    }
}
