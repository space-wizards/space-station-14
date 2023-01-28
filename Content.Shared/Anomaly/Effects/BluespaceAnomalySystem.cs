using System.Linq;
using Content.Shared.Anomaly.Components;
using Content.Shared.Anomaly.Effects.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Teleportation.Components;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
namespace Content.Shared.Anomaly.Effects;

public sealed class BluespaceAnomalySystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

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
        var range = component.MaxShuffleRadius * args.Severity;
        var allEnts = _lookup.GetComponentsInRange<MobStateComponent>(xform.Coordinates, range)
            .Select(x => x.Owner).ToList();
        if (allEnts.Count % 2 == 0) //ensure even number
            allEnts.RemoveAt(0);
        allEnts.Add(uid);

        var xformQuery = GetEntityQuery<TransformComponent>();
        var transforms = new List<TransformComponent>();
        foreach (var ent in allEnts)
        {
            if (xformQuery.TryGetComponent(ent, out var xf))
                transforms.Add(xf);
        }

        var variation = component.MaxShuffleVariation * args.Severity;

        // make sure we didn't pick up a transformless mob somehow
        while (transforms.Any())
        {
            var t1 = _random.PickAndTake(transforms);
            var t2 = _random.PickAndTake(transforms);
            var c1 = t1.Coordinates.Offset(
                new Vector2(_random.NextFloat(-variation, variation), _random.NextFloat(-variation, variation)));
            var c2 = t2.Coordinates.Offset(
                new Vector2(_random.NextFloat(-variation, variation), _random.NextFloat(-variation, variation)));

            _xform.SetCoordinates(t1, c2);
            _xform.SetCoordinates(t2, c1);
        }
    }

    private void OnSupercritical(EntityUid uid, BluespaceAnomalyComponent component, ref AnomalySupercriticalEvent args)
    {
        var xform = Transform(uid);
        var g = xform.GridUid;
        if (g is not { } grid)
            return;
        if (!TryComp<MapGridComponent>(grid, out var gridComp))
            return;
        var gridBounds = gridComp.LocalAABB;

        var xformQuery = GetEntityQuery<TransformComponent>();
        foreach (var comp in _lookup.GetComponentsInRange<MobStateComponent>(xform.Coordinates, component.MaxShuffleRadius))
        {
            var ent = comp.Owner;
            var randomX = _random.NextFloat(gridBounds.Left, gridBounds.Right);
            var randomY = _random.NextFloat(gridBounds.Bottom, gridBounds.Top);

            var pos = new Vector2(randomX, randomY);
            _xform.SetWorldPosition(ent, pos);
        }
    }

    private void OnSeverityChanged(EntityUid uid, BluespaceAnomalyComponent component, ref AnomalySeverityChangedEvent args)
    {
        if (!TryComp<PortalComponent>(uid, out var portal))
            return;
        portal.MaxRandomRadius = (component.MaxPortalRadius - component.MinPortalRadius) * args.Severity + component.MinPortalRadius;
    }
}
