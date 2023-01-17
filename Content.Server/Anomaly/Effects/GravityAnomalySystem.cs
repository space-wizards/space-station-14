using Content.Server.Singularity.Components;
using Content.Shared.Anomaly.Components;
using Content.Shared.Anomaly.Effects;
using Content.Shared.Anomaly.Effects.Components;
using Content.Shared.Radiation.Components;

namespace Content.Server.Anomaly.Effects;

/// <summary>
/// This handles logic and events relating to <see cref="GravityAnomalyComponent"/> and <seealso cref="AnomalySystem"/>
/// </summary>
public sealed class GravityAnomalySystem : SharedGravityAnomalySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GravityAnomalyComponent, AnomalySeverityChangedEvent>(OnSeverityChanged);
        SubscribeLocalEvent<GravityAnomalyComponent, AnomalyStabilityChangedEvent>(OnStabilityChanged);
    }

    private void OnSeverityChanged(EntityUid uid, GravityAnomalyComponent component, ref AnomalySeverityChangedEvent args)
    {
        if (TryComp<RadiationSourceComponent>(uid, out var radSource))
            radSource.Intensity = component.MaxRadiationIntensity * args.Severity;

        if (!TryComp<GravityWellComponent>(uid, out var gravityWell))
            return;
        var accel = (component.MaxAccel - component.MinAccel) * args.Severity + component.MinAccel;
        gravityWell.BaseRadialAcceleration = accel;
        gravityWell.BaseTangentialAcceleration = accel * 0.2f;
    }

    private void OnStabilityChanged(EntityUid uid, GravityAnomalyComponent component, ref AnomalyStabilityChangedEvent args)
    {
        if (TryComp<GravityWellComponent>(uid, out var gravityWell))
            gravityWell.MaxRange = component.MaxGravityWellRange * args.Stability;
    }
}
