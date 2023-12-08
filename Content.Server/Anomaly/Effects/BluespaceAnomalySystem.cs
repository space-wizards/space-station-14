using System.Linq;
using System.Numerics;
using Content.Server.Anomaly.Components;
using Content.Shared.Anomaly.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Teleportation.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Server.Anomaly.Effects;

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
        var xformQuery = GetEntityQuery<TransformComponent>();
        var xform = xformQuery.GetComponent(uid);
        var range = component.MaxShuffleRadius * args.Severity;
        var mobs = new HashSet<Entity<MobStateComponent>>();
        _lookup.GetEntitiesInRange(xform.Coordinates, range, mobs);
        var allEnts = new List<EntityUid>(mobs.Select(m => m.Owner)) { uid };
        var coords = new List<Vector2>();
        foreach (var ent in allEnts)
        {
            if (xformQuery.TryGetComponent(ent, out var xf))
                coords.Add(xf.MapPosition.Position);
        }

        _random.Shuffle(coords);
        for (var i = 0; i < allEnts.Count; i++)
        {
            _xform.SetWorldPosition(allEnts[i], coords[i], xformQuery);
        }
    }

    private void OnSupercritical(EntityUid uid, BluespaceAnomalyComponent component, ref AnomalySupercriticalEvent args)
    {
        var xform = Transform(uid);
        var mapPos = _xform.GetWorldPosition(xform);
        var radius = component.SupercriticalTeleportRadius;
        var gridBounds = new Box2(mapPos - new Vector2(radius, radius), mapPos + new Vector2(radius, radius));
        var mobs = new HashSet<Entity<MobStateComponent>>();
        _lookup.GetEntitiesInRange(xform.Coordinates, component.MaxShuffleRadius, mobs);
        foreach (var comp in mobs)
        {
            var ent = comp.Owner;
            var randomX = _random.NextFloat(gridBounds.Left, gridBounds.Right);
            var randomY = _random.NextFloat(gridBounds.Bottom, gridBounds.Top);

            var pos = new Vector2(randomX, randomY);
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
}
