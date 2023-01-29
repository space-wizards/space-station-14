using System.Linq;
using Content.Shared.Anomaly.Components;
using Content.Shared.Anomaly.Effects.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Teleportation.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
namespace Content.Shared.Anomaly.Effects;

public sealed class BluespaceAnomalySystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
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
        allEnts.Add(uid);

        var xformQuery = GetEntityQuery<TransformComponent>();
        var coords = new List<EntityCoordinates>();
        foreach (var ent in allEnts)
        {
            if (xformQuery.TryGetComponent(ent, out var xf))
                coords.Add(xf.Coordinates);
        }

        while (allEnts.Any())
        {
            var ent = _random.PickAndTake(allEnts);
            var coord = _random.PickAndTake(coords);

            _xform.SetCoordinates(ent, coord);
            _audio.PlayPvs(component.TeleportSound, ent);
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

        foreach (var comp in _lookup.GetComponentsInRange<MobStateComponent>(xform.Coordinates, component.MaxShuffleRadius))
        {
            var ent = comp.Owner;
            var randomX = _random.NextFloat(gridBounds.Left, gridBounds.Right);
            var randomY = _random.NextFloat(gridBounds.Bottom, gridBounds.Top);

            var pos = new EntityCoordinates(grid, new Vector2(randomX, randomY));
            _xform.SetCoordinates(ent, pos);
            _audio.PlayPvs(component.TeleportSound, ent);
        }
    }

    private void OnSeverityChanged(EntityUid uid, BluespaceAnomalyComponent component, ref AnomalySeverityChangedEvent args)
    {
        if (!TryComp<PortalComponent>(uid, out var portal))
            return;
        portal.MaxRandomRadius = (component.MaxPortalRadius - component.MinPortalRadius) * args.Severity + component.MinPortalRadius;
    }
}
