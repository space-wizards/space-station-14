using System.Linq;
using Content.Server.Maps;
using Content.Shared.Anomaly.Components;
using Content.Shared.Anomaly.Effects.Components;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;

namespace Content.Server.Anomaly.Effects;

public sealed class FleshAnomalySystem : EntitySystem
{
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ITileDefinitionManager _tiledef = default!;
    [Dependency] private readonly TileSystem _tile = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<FleshAnomalyComponent, AnomalyPulseEvent>(OnPulse);
        SubscribeLocalEvent<FleshAnomalyComponent, AnomalySupercriticalEvent>(OnSupercritical);
        SubscribeLocalEvent<FleshAnomalyComponent, AnomalyStabilityChangedEvent>(OnSeverityChanged);
    }

    private void OnPulse(EntityUid uid, FleshAnomalyComponent component, ref AnomalyPulseEvent args)
    {
        var range = component.SpawnRange * args.Stability;
        var amount = (int) (component.MaxSpawnAmount * args.Severity + 0.5f);

        var xform = Transform(uid);
        SpawnMonstersOnOpenTiles(component, xform, amount, range);
    }

    private void OnSupercritical(EntityUid uid, FleshAnomalyComponent component, ref AnomalySupercriticalEvent args)
    {
        var xform = Transform(uid);
        SpawnMonstersOnOpenTiles(component, xform, component.MaxSpawnAmount, component.SpawnRange);
        Spawn(component.SupercriticalSpawn, xform.Coordinates);
    }

    private void OnSeverityChanged(EntityUid uid, FleshAnomalyComponent component, ref AnomalyStabilityChangedEvent args)
    {
        var xform = Transform(uid);
        if (!_map.TryGetGrid(xform.GridUid, out var grid))
            return;

        var radius = component.SpawnRange * args.Stability;
        var fleshTile = (ContentTileDefinition) _tiledef[component.FleshTileId];
        var localpos = xform.Coordinates.Position;
        var tilerefs = grid.GetLocalTilesIntersecting(
            new Box2(localpos + (-radius, -radius), localpos + (radius, radius)));
        foreach (var tileref in tilerefs)
        {
            if (!_random.Prob(0.33f))
                continue;
            _tile.ReplaceTile(tileref, fleshTile);
        }
    }

    private void SpawnMonstersOnOpenTiles(FleshAnomalyComponent component, TransformComponent xform, int amount, float radius)
    {
        if (!_map.TryGetGrid(xform.GridUid, out var grid))
            return;

        var localpos = xform.Coordinates.Position;
        var tilerefs = grid.GetLocalTilesIntersecting(
            new Box2(localpos + (-radius, -radius), localpos + (radius, radius))).ToArray();
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
            Spawn(_random.Pick(component.Spawns), tileref.GridIndices.ToEntityCoordinates(xform.GridUid.Value, _map));
            if (amountCounter >= amount)
                return;
        }
    }
}
