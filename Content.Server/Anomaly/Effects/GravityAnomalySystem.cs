using Content.Server.Physics.Components;
using Content.Server.Radiation.Systems;
using Content.Server.Singularity.Components;
using Content.Shared.Anomaly.Components;
using Content.Shared.Anomaly.Effects;
using Content.Shared.Anomaly.Effects.Components;

namespace Content.Server.Anomaly.Effects;

/// <summary>
/// This handles logic and events relating to <see cref="GravityAnomalyComponent"/> and <seealso cref="AnomalySystem"/>
/// </summary>
public sealed class GravityAnomalySystem : SharedGravityAnomalySystem
{
    [Dependency] private readonly RadiationSystem _radiation = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GravityAnomalyComponent, AnomalySeverityChangedEvent>(OnSeverityChanged);
        SubscribeLocalEvent<GravityAnomalyComponent, AnomalyStabilityChangedEvent>(OnStabilityChanged);
    }

    private void OnSeverityChanged(Entity<GravityAnomalyComponent> anomaly, ref AnomalySeverityChangedEvent args)
    {
        _radiation.SetIntensity(anomaly.Owner, anomaly.Comp.MaxRadiationIntensity * args.Severity);

        if (TryComp<GravityWellComponent>(anomaly, out var gravityWell))
        {
            var accel = MathHelper.Lerp(anomaly.Comp.MinAccel, anomaly.Comp.MaxAccel, args.Severity);
            gravityWell.BaseRadialAcceleration = accel;

            var radialAccel = MathHelper.Lerp(anomaly.Comp.MinRadialAccel, anomaly.Comp.MaxRadialAccel, args.Severity);
            gravityWell.BaseTangentialAcceleration = radialAccel;
        }

        if (TryComp<RandomWalkComponent>(anomaly, out var randomWalk))
        {
            var speed = MathHelper.Lerp(anomaly.Comp.MinSpeed, anomaly.Comp.MaxSpeed, args.Severity);
            randomWalk.MinSpeed = speed - anomaly.Comp.SpeedVariation;
            randomWalk.MaxSpeed = speed + anomaly.Comp.SpeedVariation;
        }
    }

    private void OnStabilityChanged(Entity<GravityAnomalyComponent> anomaly, ref AnomalyStabilityChangedEvent args)
    {
        if (TryComp<GravityWellComponent>(anomaly, out var gravityWell))
            gravityWell.MaxRange = anomaly.Comp.MaxGravityWellRange * args.Stability;
    }
}
