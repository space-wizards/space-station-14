using Content.Server.Anomaly.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Anomaly;
using Content.Shared.Anomaly.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Research.Components;
using Robust.Shared.Timing;

namespace Content.Server.Anomaly;

/// <summary>
/// This handles anomalous vessel as well as
/// the calculations for how many points they
/// should produce.
/// </summary>
public sealed partial class AnomalySystem
{
    private static readonly EntityTimerId VesselBeepTimer = new("vessel-beep");

    private void InitializeVessel()
    {
        SubscribeLocalEvent<AnomalyVesselComponent, ComponentShutdown>(OnVesselShutdown);
        SubscribeLocalEvent<AnomalyVesselComponent, MapInitEvent>(OnVesselMapInit);
        SubscribeLocalEvent<AnomalyVesselComponent, InteractUsingEvent>(OnVesselInteractUsing);
        SubscribeLocalEvent<AnomalyVesselComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<AnomalyVesselComponent, ResearchServerGetPointsPerSecondEvent>(OnVesselGetPointsPerSecond);
        SubscribeLocalEvent<AnomalyVesselComponent, EntityTimerEvent>(OnVesselBeepTimer);
        SubscribeLocalEvent<AnomalyShutdownEvent>(OnVesselAnomalyShutdown);
    }

    private void OnExamined(EntityUid uid, AnomalyVesselComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushText(component.Anomaly == null
            ? Loc.GetString("anomaly-vessel-component-not-assigned")
            : Loc.GetString("anomaly-vessel-component-assigned"));
    }

    private void OnVesselShutdown(EntityUid uid, AnomalyVesselComponent component, ComponentShutdown args)
    {
        if (component.Anomaly is not { } anomaly)
            return;

        if (!TryComp<AnomalyComponent>(anomaly, out var anomalyComp))
            return;

        anomalyComp.ConnectedVessel = null;
    }

    private void OnVesselMapInit(EntityUid uid, AnomalyVesselComponent component, MapInitEvent args)
    {
        UpdateVesselAppearance(uid,  component);
        ScheduleVesselBeep((uid, component));
    }

    private void OnVesselInteractUsing(EntityUid uid, AnomalyVesselComponent component, InteractUsingEvent args)
    {
        if (component.Anomaly != null ||
            !TryComp<AnomalyScannerComponent>(args.Used, out var scanner) ||
            scanner.ScannedAnomaly is not { } anomaly)
        {
            return;
        }

        if (!TryComp<AnomalyComponent>(anomaly, out var anomalyComponent) || anomalyComponent.ConnectedVessel != null)
            return;

        component.Anomaly = scanner.ScannedAnomaly;
        anomalyComponent.ConnectedVessel = uid;
        _radiation.SetSourceEnabled(uid, true);
        UpdateVesselAppearance(uid,  component);
        ScheduleVesselBeep((uid, component), immediate: true);
        Popup.PopupEntity(Loc.GetString("anomaly-vessel-component-anomaly-assigned"), uid);
    }

    private void OnVesselGetPointsPerSecond(EntityUid uid, AnomalyVesselComponent component, ref ResearchServerGetPointsPerSecondEvent args)
    {
        if (!this.IsPowered(uid, EntityManager) || component.Anomaly is not {} anomaly)
            return;

        args.Points += (int) (GetAnomalyPointValue(anomaly) * component.PointMultiplier);
    }

    private void OnVesselAnomalyShutdown(ref AnomalyShutdownEvent args)
    {
        var query = EntityQueryEnumerator<AnomalyVesselComponent>();
        while (query.MoveNext(out var ent, out var component))
        {
            if (args.Anomaly != component.Anomaly)
                continue;

            component.Anomaly = null;
            _timers.CancelTimer<AnomalyVesselComponent>(ent, VesselBeepTimer);
            UpdateVesselAppearance(ent,  component);
            _radiation.SetSourceEnabled(ent, false);

            if (!args.Supercritical)
                continue;
            _explosion.TriggerExplosive(ent);
        }
    }

    private void OnVesselAnomalyStabilityChanged(ref AnomalyStabilityChangedEvent args)
    {
        var query = EntityQueryEnumerator<AnomalyVesselComponent>();
        while (query.MoveNext(out var ent, out var component))
        {
            if (args.Anomaly != component.Anomaly)
                continue;

            UpdateVesselAppearance(ent,  component);
            ScheduleVesselBeep((ent, component), immediate: true);
        }
    }

    /// <summary>
    /// Updates the appearance of an anomaly vessel
    /// based on whether or not it has an anomaly
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    public void UpdateVesselAppearance(EntityUid uid, AnomalyVesselComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var on = component.Anomaly != null;

        if (!TryComp<AppearanceComponent>(uid, out var appearanceComponent))
            return;

        Appearance.SetData(uid, AnomalyVesselVisuals.HasAnomaly, on, appearanceComponent);
        if (_pointLight.TryGetLight(uid, out var pointLightComponent))
            _pointLight.SetEnabled(uid, on, pointLightComponent);

        if (component.Anomaly == null || !TryGetStabilityVisual(component.Anomaly.Value, out var visual))
            visual = AnomalyStabilityVisuals.Stable;

        Appearance.SetData(uid, AnomalyVesselVisuals.AnomalySeverity, visual, appearanceComponent);

        _ambient.SetAmbience(uid, on);
    }

    private void OnVesselBeepTimer(Entity<AnomalyVesselComponent> vessel, ref EntityTimerEvent args)
    {
        if (args.Id != VesselBeepTimer || vessel.Comp.Anomaly is not { } anomalyUid ||
            !TryComp<AnomalyComponent>(anomalyUid, out var anomaly))
            return;

        if (!TryGetVesselTimerPercentage(anomaly, out var timerPercentage))
            return;

        Audio.PlayPvs(vessel.Comp.BeepSound, vessel);
        var interval = (vessel.Comp.MaxBeepInterval - vessel.Comp.MinBeepInterval) * (1 - timerPercentage) +
                       vessel.Comp.MinBeepInterval;
        vessel.Comp.NextBeep = args.FiredAt + interval;
        _timers.SetTimerAt(vessel, VesselBeepTimer, vessel.Comp.NextBeep);
    }

    private void ScheduleVesselBeep(Entity<AnomalyVesselComponent> vessel, bool immediate = false)
    {
        if (vessel.Comp.Anomaly is not { } anomalyUid || !TryComp<AnomalyComponent>(anomalyUid, out var anomaly) ||
            !TryGetVesselTimerPercentage(anomaly, out _))
        {
            _timers.CancelTimer<AnomalyVesselComponent>(vessel, VesselBeepTimer);
            return;
        }

        var deadline = immediate ? Timing.CurTime : vessel.Comp.NextBeep;
        _timers.SetTimerAt(vessel, VesselBeepTimer, deadline);
    }

    private static bool TryGetVesselTimerPercentage(AnomalyComponent anomaly, out float timerPercentage)
    {
        if (anomaly.Stability <= anomaly.DecayThreshold)
            timerPercentage = (anomaly.DecayThreshold - anomaly.Stability) / anomaly.DecayThreshold;
        else if (anomaly.Stability >= anomaly.GrowthThreshold)
            timerPercentage = (anomaly.Stability - anomaly.GrowthThreshold) / (1 - anomaly.GrowthThreshold);
        else
        {
            timerPercentage = default;
            return false;
        }

        return true;
    }
}
