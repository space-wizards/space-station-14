using Content.Server.Administration.Logs;
using Content.Server.Anomaly.Components;
using Content.Server.DoAfter;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Popups;
using Content.Shared.Anomaly;
using Content.Shared.Database;
using Robust.Server.GameObjects;
using Robust.Shared.Physics.Events;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Anomaly;

/// <summary>
/// This handles logic and interactions relating to <see cref="AnomalyComponent"/>
/// </summary>
public sealed partial class AnomalySystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _log = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<AnomalyComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AnomalyComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<AnomalyComponent, EntityUnpausedEvent>(OnUnpause);
        SubscribeLocalEvent<AnomalyComponent, StartCollideEvent>(OnStartCollide);

        InitializeScanner();
        InitializeVessel();
    }

    private void OnMapInit(EntityUid uid, AnomalyComponent component, MapInitEvent args)
    {
        component.Stability = _random.NextFloat(component.InitialStabilityRange.Item1, component.InitialStabilityRange.Item2);
        component.Severity = _random.NextFloat(component.InitialSeverityRange.Item1, component.InitialSeverityRange.Item2);
        component.NextPulseTime = _timing.CurTime + GetPulseLength(component) * 2; //extra long the first time

        var particles = new List<AnomalousParticleType>
            { AnomalousParticleType.Delta, AnomalousParticleType.Epsilon, AnomalousParticleType.Zeta };
        component.SeverityParticleType = _random.PickAndTake(particles);
        component.DestabilizingParticleType = _random.PickAndTake(particles);
        component.WeakeningParticleType = _random.PickAndTake(particles);
    }

    private void OnShutdown(EntityUid uid, AnomalyComponent component, ComponentShutdown args)
    {
        EndAnomaly(uid, component);
    }

    private void OnUnpause(EntityUid uid, AnomalyComponent component, ref EntityUnpausedEvent args)
    {
        component.NextPulseTime += args.PausedTime;
        component.NextSecondUpdate += args.PausedTime;
    }

    private void OnStartCollide(EntityUid uid, AnomalyComponent component, ref StartCollideEvent args)
    {
        if (!TryComp<AnomalousParticleComponent>(args.OtherFixture.Body.Owner, out var particleComponent))
            return;

        if (args.OtherFixture.ID != particleComponent.FixtureId)
            return;

        if (particleComponent.ParticleType == component.DestabilizingParticleType)
        {
            ChangeAnomalyStability(uid, component.StabilityPerDestabilizingHit, component);
        }
        else if (particleComponent.ParticleType == component.SeverityParticleType)
        {
            ChangeAnomalySeverity(uid, component.SeverityPerSeverityHit, component);
        }
        else if (particleComponent.ParticleType == component.WeakeningParticleType)
        {
            ChangeAnomalyHealth(uid, component.HealthPerWeakeningeHit, component);
            ChangeAnomalyStability(uid, component.StabilityPerWeakeningeHit, component);
        }
    }


    public void DoAnomalyPulse(EntityUid uid, AnomalyComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var variation = _random.NextFloat(-component.PulseVariation, component.PulseVariation) + 1;
        component.NextPulseTime = _timing.CurTime + GetPulseLength(component) * variation;

        // if we are above the growth threshold, then grow before the pulse
        if (component.Stability > component.GrowthThreshold)
        {
            ChangeAnomalySeverity(uid, GetSeverityIncreaseFromGrowth(component), component);
            // if growth caused the anomaly to go supercritical,
            // then we need to cancel the pulse early so we don't error.
            if (component.Supercritical)
                return;
        }

        _log.Add(LogType.Anomaly, LogImpact.Medium, $"Anomaly {ToPrettyString(uid)} pulsed with severity {component.Severity}.");

        var ev = new AnomalyPulseEvent(component.Stability, component.Severity);
        RaiseLocalEvent(uid, ref ev);
    }

    public void DoAnomalySupercriticalEvent(EntityUid uid, AnomalyComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.Supercritical)
            return;
        component.Supercritical = true;

        _log.Add(LogType.Anomaly, LogImpact.Extreme, $"Anomaly {ToPrettyString(uid)} went Supercritical.");

        //TODO: sfx?
        var ev = new AnomalySupercriticalEvent();
        RaiseLocalEvent(uid, ref ev);

        EndAnomaly(uid, component, true);
    }

    /// <summary>
    /// Ends an anomaly, cleaning up all entities that may be associated with it.
    /// </summary>
    /// <param name="uid">The anomaly being shut down</param>
    /// <param name="component"></param>
    /// <param name="supercritical">Whether or not the anomaly ended via supercritical event</param>
    public void EndAnomaly(EntityUid uid, AnomalyComponent? component = null, bool supercritical = false)
    {
        if (!Resolve(uid, ref component))
            return;

        var ev = new AnomalyShutdownEvent(uid, supercritical);
        RaiseLocalEvent(ref ev);

        if (Terminating(uid))
            return;
        //TODO: we might want to have some cool visual effect here.
        Del(uid);
    }

    /// <summary>
    /// Changes the stability of the anomaly.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="change"></param>
    /// <param name="component"></param>
    public void ChangeAnomalyStability(EntityUid uid, float change, AnomalyComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var newVal = component.Stability + change;

        component.Stability = Math.Clamp(newVal, 0, 1);
    }

    /// <summary>
    /// Changes the severity of an anomaly, going supercritical if it exceeds 1.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="change"></param>
    /// <param name="component"></param>
    public void ChangeAnomalySeverity(EntityUid uid, float change, AnomalyComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var newVal = component.Severity + change;

        if (newVal > 1)
            DoAnomalySupercriticalEvent(uid, component);

        component.Severity = Math.Clamp(newVal, 0, 1);
    }

    /// <summary>
    /// Changes the health of an anomaly, ending it if it's less than 0.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="change"></param>
    /// <param name="component"></param>
    public void ChangeAnomalyHealth(EntityUid uid, float change, AnomalyComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var newVal = component.Health + change;

        if (newVal < 0)
            EndAnomaly(uid, component);

        component.Health = Math.Clamp(newVal, 0, 1);
    }

    /// <summary>
    /// Gets the length of time between each pulse
    /// for an anomaly based on its current stability.
    /// </summary>
    /// <remarks>
    /// For anomalies under the instability theshold, this will return the maximum length.
    /// For those over the theshold, they will return an amount between the maximum and
    /// minium value based on a linear relationship with the stability.
    /// </remarks>
    /// <param name="component"></param>
    /// <returns>The length of time as a TimeSpan, not including random variation.</returns>
    public TimeSpan GetPulseLength(AnomalyComponent component)
    {
        DebugTools.Assert(component.MaxPulseLength > component.MinPulseLength);
        var modifier = Math.Clamp((component.Stability - component.GrowthThreshold) /  component.GrowthThreshold, 0, 1);
        return (component.MaxPulseLength - component.MinPulseLength) * modifier + component.MinPulseLength;
    }

    /// <summary>
    /// Gets the increase in an anomaly's severity due
    /// to being above its growth threshold
    /// </summary>
    /// <param name="component"></param>
    /// <returns>The increase in severity for this anomaly</returns>
    private float GetSeverityIncreaseFromGrowth(AnomalyComponent component)
    {
        var score = 1 + Math.Max(component.Stability - component.GrowthThreshold, 0) * 10;
        return score * component.SeverityGrowthCoefficient;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var anomaly in EntityQuery<AnomalyComponent>())
        {
            // if the stability is under the death threshold,
            // update it every second to start killing it slowly.
            if (anomaly.Stability < anomaly.DeathThreshold &&
                anomaly.NextSecondUpdate <= _timing.CurTime)
            {
                ChangeAnomalyHealth(anomaly.Owner, anomaly.HealthChangePerSecond, anomaly);
                anomaly.NextSecondUpdate = _timing.CurTime + TimeSpan.FromSeconds(1);
            }

            if (anomaly.NextPulseTime <= _timing.CurTime)
            {
                DoAnomalyPulse(anomaly.Owner, anomaly);
            }
        }
    }
}
