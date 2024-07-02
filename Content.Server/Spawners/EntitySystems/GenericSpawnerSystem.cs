using System.Linq;
using System.Numerics;
using Content.Server.GameTicking;
using Content.Server.Spawners.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Spawners.EntitySystems;

/// <summary>
/// This handles spawner markers.
/// </summary>
public sealed class GenericSpawnerSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GameRuleStartedEvent>(OnRuleStarted);
        SubscribeLocalEvent<GenericSpawnerComponent, MapInitEvent>(OnSpawnMapInit);
    }

    private void OnSpawnMapInit(EntityUid uid, GenericSpawnerComponent component, MapInitEvent args)
    {
        TrySpawn(uid, component);
        if (component.DeleteSpawnerAfterSpawn)
            QueueDel(uid);
    }

    private void OnRuleStarted(ref GameRuleStartedEvent args)
    {
        var query = EntityQueryEnumerator<GenericSpawnerComponent>();
        while (query.MoveNext(out var uid, out var spawner))
        {
            RuleStarted(uid, spawner, args);
        }
    }

    public void RuleStarted(EntityUid uid, GenericSpawnerComponent component, GameRuleStartedEvent obj)
    {
        if (component.GameRules.Contains(obj.RuleId))
            Spawn(uid, component);
    }

    private void TrySpawn(EntityUid uid, GenericSpawnerComponent component)
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

    private void Spawn(EntityUid uid, GenericSpawnerComponent component)
    {
        if (Deleted(uid))
            return;

        if (component.Chance != 1.0f && !_robustRandom.Prob(component.Chance))
            return;

        if (!_proto.TryIndex(component.EntityTable, out var entTable))
        {
            Log.Warning($"Referenced entity table prototype does not exist! Entity: {ToPrettyString(uid)}");
            return;
        }

        if (entTable.Weights.Count == 0)
        {
            Log.Warning($"Entity table in GenericSpawnerComponent is empty! Entity: {ToPrettyString(uid)}");
            return;
        }

        if (component.Rolls is < 1 or > 100)
        {
            Log.Warning($"Invalid amount of rolls on entity table, value should be between 1 and 100. Entity: {ToPrettyString(uid)}");
            return;
        }

        foreach (var _ in Enumerable.Repeat(1, component.Rolls))
        {
            var entity = entTable.Pick(_robustRandom);
            var offset = component.Offset;
            var xOffset = _robustRandom.NextFloat(-offset, offset);
            var yOffset = _robustRandom.NextFloat(-offset, offset);
            var coordinates = Transform(uid).Coordinates.Offset(new Vector2(xOffset, yOffset));

            EntityManager.SpawnEntity(entity, coordinates);
        }
    }
}
