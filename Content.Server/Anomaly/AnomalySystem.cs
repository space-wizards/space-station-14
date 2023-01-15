using Content.Server.Administration.Logs;
using Content.Server.Anomaly.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Audio;
using Content.Server.DoAfter;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Materials;
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
public sealed partial class AnomalySystem : SharedAnomalySystem
{
    [Dependency] private readonly IAdminLogManager _log = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly AmbientSoundSystem _ambient = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly MaterialStorageSystem _material = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public const float MinParticleVariation = 0.8f;
    public const float MaxParticleVariation = 1.2f;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AnomalyComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AnomalyComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<AnomalyComponent, EntityUnpausedEvent>(OnUnpause);
        SubscribeLocalEvent<AnomalyComponent, StartCollideEvent>(OnStartCollide);

        InitializeGenerator();
        InitializeScanner();
        InitializeVessel();
    }

    private void OnMapInit(EntityUid uid, AnomalyComponent component, MapInitEvent args)
    {
        component.Stability = _random.NextFloat(component.InitialStabilityRange.Item1, component.InitialStabilityRange.Item2);
        component.Severity = _random.NextFloat(component.InitialSeverityRange.Item1, component.InitialSeverityRange.Item2);
        component.NextPulseTime = _timing.CurTime + GetPulseLength(component) * 2; // longer the first time

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
    }

    private void OnStartCollide(EntityUid uid, AnomalyComponent component, ref StartCollideEvent args)
    {
        if (!TryComp<AnomalousParticleComponent>(args.OtherFixture.Body.Owner, out var particleComponent))
            return;

        if (args.OtherFixture.ID != particleComponent.FixtureId)
            return;

        // small function to randomize because it's easier to read like this
        float VaryValue(float v) => v * _random.NextFloat(MinParticleVariation, MaxParticleVariation);

        if (particleComponent.ParticleType == component.DestabilizingParticleType)
        {
            ChangeAnomalyStability(uid, VaryValue(component.StabilityPerDestabilizingHit), component);
        }
        else if (particleComponent.ParticleType == component.SeverityParticleType)
        {
            ChangeAnomalySeverity(uid, VaryValue(component.SeverityPerSeverityHit), component);
        }
        else if (particleComponent.ParticleType == component.WeakeningParticleType)
        {
            ChangeAnomalyHealth(uid, VaryValue(component.HealthPerWeakeningeHit), component);
            ChangeAnomalyStability(uid, VaryValue(component.StabilityPerWeakeningeHit), component);
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
        }
        else
        {
            // just doing this to update the scanner ui
            // as they hook into these events
            ChangeAnomalySeverity(uid, 0);
        }

        _log.Add(LogType.Anomaly, LogImpact.Medium, $"Anomaly {ToPrettyString(uid)} pulsed with severity {component.Severity}.");
        _audio.PlayPvs(component.PulseSound, uid);

        var pulse = EnsureComp<AnomalyPulsingComponent>(uid);
        pulse.PulseEndTime  = _timing.CurTime + pulse.PulseDuration;
        _appearance.SetData(uid, AnomalyVisuals.IsPulsing, true);

        var ev = new AnomalyPulseEvent(component.Stability, component.Severity);
        RaiseLocalEvent(uid, ref ev);
    }

    /// <summary>
    /// Begins the animation for going supercritical
    /// </summary>
    /// <param name="uid"></param>
    public void StartSupercriticalEvent(EntityUid uid)
    {
        // don't restart it if it's already begun
        if (HasComp<AnomalySupercriticalComponent>(uid))
            return;

        var super = EnsureComp<AnomalySupercriticalComponent>(uid);
        super.EndTime = _timing.CurTime + super.SupercriticalDuration;
        _appearance.SetData(uid, AnomalyVisuals.Supercritical, true);
        Dirty(super);
    }

    /// <summary>
    /// Does the supercritical event for the anomaly.
    /// This isn't called once the anomaly reaches the point, but
    /// after the animation for it going supercritical
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    public void DoAnomalySupercriticalEvent(EntityUid uid, AnomalyComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        _log.Add(LogType.Anomaly, LogImpact.Extreme, $"Anomaly {ToPrettyString(uid)} went Supercritical.");
        _audio.PlayPvs(component.SupercriticalSound, uid);

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

        var ev = new AnomalyStabilityChangedEvent(uid);
        RaiseLocalEvent(ref ev);
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

        if (newVal >= 1)
            StartSupercriticalEvent(uid);

        component.Severity = Math.Clamp(newVal, 0, 1);

        var ev = new AnomalySeverityChangedEvent(uid);
        RaiseLocalEvent(ref ev);
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
        {
            EndAnomaly(uid, component);
            return;
        }

        component.Health = Math.Clamp(newVal, 0, 1);

        var ev = new AnomalyHealthChangedEvent(uid);
        RaiseLocalEvent(ref ev);
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

    /// <summary>
    /// Gets the amount of research points generated per second for an anomaly.
    /// </summary>
    /// <param name="anomaly"></param>
    /// <param name="component"></param>
    /// <returns>The amount of points</returns>
    public int GetAnomalyPointValue(EntityUid anomaly, AnomalyComponent? component = null)
    {
        if (!Resolve(anomaly, ref component, false))
            return 0;

        var multiplier = 1f;
        if (component.Stability > component.GrowthThreshold)
            multiplier = 1.25f; //more points for unstable
        else if (component.Stability < component.DecayThreshold)
            multiplier = 0.75f; //less points if it's dying

        //penalty of up to 50% based on health
        multiplier *= MathF.Pow(1.5f, component.Health) - 0.5f;

        return (int) ((component.MaxPointsPerSecond - component.MinPointsPerSecond) * component.Severity * multiplier);
    }

    /// <summary>
    /// Gets the localized name of a particle.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public string GetParticleLocale(AnomalousParticleType type)
    {
        return type switch
        {
            AnomalousParticleType.Delta => Loc.GetString("anomaly-particles-delta"),
            AnomalousParticleType.Epsilon => Loc.GetString("anomaly-particles-epsilon"),
            AnomalousParticleType.Zeta => Loc.GetString("anomaly-particles-zeta"),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var anomaly in EntityQuery<AnomalyComponent>())
        {
            var ent = anomaly.Owner;

            // if the stability is under the death threshold,
            // update it every second to start killing it slowly.
            if (anomaly.Stability < anomaly.DecayThreshold)
            {
                ChangeAnomalyHealth(ent, anomaly.HealthChangePerSecond * frameTime, anomaly);
            }

            if (_timing.CurTime > anomaly.NextPulseTime)
            {
                DoAnomalyPulse(ent, anomaly);
            }
        }

        foreach (var pulse in EntityQuery<AnomalyPulsingComponent>())
        {
            var ent = pulse.Owner;

            if (_timing.CurTime > pulse.PulseEndTime)
            {
                _appearance.SetData(ent, AnomalyVisuals.IsPulsing, false);
                RemComp(ent, pulse);
            }
        }

        foreach (var (super, anom) in EntityQuery<AnomalySupercriticalComponent, AnomalyComponent>())
        {
            var ent = anom.Owner;

            if (_timing.CurTime <= super.EndTime)
                continue;
            DoAnomalySupercriticalEvent(ent, anom);
            RemComp(ent, super);
        }
    }
}
