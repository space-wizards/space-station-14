using System.Linq;
using System.Numerics;
using Content.Shared.Anomaly.Components;
using Content.Shared.Anomaly.Effects.Components;
using Content.Shared.Physics;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Anomaly.Effects;

public sealed class EntityAnomalySystem : EntitySystem
{
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<EntitySpawnAnomalyComponent, AnomalyPulseEvent>(OnPulse);
        SubscribeLocalEvent<EntitySpawnAnomalyComponent, AnomalySupercriticalEvent>(OnSupercritical);
        SubscribeLocalEvent<EntitySpawnAnomalyComponent, AnomalyStabilityChangedEvent>(OnStabilityChanged);
    }

    private void OnPulse(EntityUid uid, EntitySpawnAnomalyComponent component, ref AnomalyPulseEvent args)
    {
        if (!component.SpawnOnPulse)
            return;

        var range = component.SpawnRange * args.Stability;
        var amount = (int) (component.MaxSpawnAmount * args.Severity + 0.5f);

        var xform = Transform(uid);
        SpawnEntitesOnOpenTiles(component, xform, amount, range, component.Spawns);
    }

    private void OnSupercritical(EntityUid uid, EntitySpawnAnomalyComponent component, ref AnomalySupercriticalEvent args)
    {
        if (!component.SpawnOnSuperCritical)
            return;

        var xform = Transform(uid);
        // A cluster of entities
        SpawnEntitesOnOpenTiles(component, xform, component.MaxSpawnAmount, component.SpawnRange, component.Spawns);
        // And so much meat (for the meat anomaly at least)
        SpawnEntitesOnOpenTiles(component, xform, component.MaxSpawnAmount, component.SpawnRange, component.SuperCriticalSpawns);
    }

    private void OnStabilityChanged(EntityUid uid, EntitySpawnAnomalyComponent component, ref AnomalyStabilityChangedEvent args)
    {
        if (!component.SpawnOnStabilityChanged)
            return;

        var range = component.SpawnRange * args.Stability;
        var amount = (int) (component.MaxSpawnAmount * args.Stability + 0.5f);

        var xform = Transform(uid);
        SpawnEntitesOnOpenTiles(component, xform, amount, range, component.Spawns);
    }

    private void SpawnEntitesOnOpenTiles(EntitySpawnAnomalyComponent component, TransformComponent xform, int amount, float radius, List<EntProtoId> spawns)
    {
        if (!component.Spawns.Any())
            return;

        if (!_map.TryGetGrid(xform.GridUid, out var grid))
            return;

        var localpos = xform.Coordinates.Position;
        var tilerefs = grid.GetLocalTilesIntersecting(
            new Box2(localpos + new Vector2(-radius, -radius), localpos + new Vector2(radius, radius))).ToArray();

        if (tilerefs.Length == 0)
            return;

        _random.Shuffle(tilerefs);
        var physQuery = GetEntityQuery<PhysicsComponent>();
        var amountCounter = 0;
        foreach (var tileref in tilerefs)
        {
            var valid = true;
            foreach (var ent in grid.GetAnchoredEntities(tileref.GridIndices))
            {
                if (!physQuery.TryGetComponent(ent, out var body))
                    continue;

                if (body.BodyType != BodyType.Static ||
                    !body.Hard ||
                    (body.CollisionLayer & (int) CollisionGroup.Impassable) == 0)
                    continue;

                valid = false;
                break;
            }
            if (!valid)
                continue;
            amountCounter++;
            Spawn(_random.Pick(spawns), tileref.GridIndices.ToEntityCoordinates(xform.GridUid.Value, _map));
            if (amountCounter >= amount)
                return;
        }
    }
}
