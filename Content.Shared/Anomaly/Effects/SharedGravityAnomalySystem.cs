using Content.Shared.Anomaly.Components;
using Content.Shared.Anomaly.Effects.Components;
using Content.Shared.Throwing;

namespace Content.Shared.Anomaly.Effects;

public abstract class SharedGravityAnomalySystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<GravityAnomalyComponent, AnomalyPulseEvent>(OnAnomalyPulse);
        SubscribeLocalEvent<GravityAnomalyComponent, AnomalySupercriticalEvent>(OnSupercritical);
    }

    private void OnAnomalyPulse(EntityUid uid, GravityAnomalyComponent component, ref AnomalyPulseEvent args)
    {
        var xform = Transform(uid);
        var range = component.MaxThrowRange * args.Severity;
        var strength = component.MaxThrowStrength * args.Severity;
        var lookup = _lookup.GetEntitiesInRange(uid, range, LookupFlags.Dynamic | LookupFlags.Sundries);
        foreach (var ent in lookup)
        {
            var tempXform = Transform(ent);

            var foo = tempXform.MapPosition.Position - xform.MapPosition.Position;
            _throwing.TryThrow(ent, foo.Normalized * 10, strength, uid, 0);
        }
    }

    private void OnSupercritical(EntityUid uid, GravityAnomalyComponent component, ref AnomalySupercriticalEvent args)
    {

    }
}
