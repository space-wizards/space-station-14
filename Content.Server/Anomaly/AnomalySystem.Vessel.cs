using Content.Server.Anomaly.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Anomaly;
using Content.Shared.Anomaly.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Research.Components;

namespace Content.Server.Anomaly;

/// <summary>
/// This handles anomalous vessel as well as
/// the calculations for how many points they
/// should produce.
/// </summary>
public sealed partial class AnomalySystem
{
    private void InitializeVessel()
    {
        SubscribeLocalEvent<AnomalyVesselComponent, ComponentShutdown>(OnVesselShutdown);
        SubscribeLocalEvent<AnomalyVesselComponent, MapInitEvent>(OnVesselMapInit);
        SubscribeLocalEvent<AnomalyVesselComponent, InteractUsingEvent>(OnVesselInteractUsing);
        SubscribeLocalEvent<AnomalyVesselComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<AnomalyVesselComponent, ResearchServerGetPointsPerSecondEvent>(OnVesselGetPointsPerSecond);
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

    private void UpdateVessels()
    {
        var query = EntityQueryEnumerator<AnomalyVesselComponent>();
        while (query.MoveNext(out var vesselEnt, out var vessel))
        {
            if (vessel.Anomaly is not { } anomUid)
                continue;

            if (!TryComp<AnomalyComponent>(anomUid, out var anomaly))
                continue;

            if (Timing.CurTime < vessel.NextBeep)
                continue;

            // a lerp between the max and min values for each threshold.
            // longer beeps that get shorter as the anomaly gets more extreme
            float timerPercentage;
            if (anomaly.Stability <= anomaly.DecayThreshold)
                timerPercentage = (anomaly.DecayThreshold - anomaly.Stability) / anomaly.DecayThreshold;
            else if (anomaly.Stability >= anomaly.GrowthThreshold)
                timerPercentage = (anomaly.Stability - anomaly.GrowthThreshold) / (1 - anomaly.GrowthThreshold);
            else //it's not unstable
                continue;

            Audio.PlayPvs(vessel.BeepSound, vesselEnt);
            var beepInterval = (vessel.MaxBeepInterval - vessel.MinBeepInterval) * (1 - timerPercentage) + vessel.MinBeepInterval;
            vessel.NextBeep = beepInterval + Timing.CurTime;
        }
    }
}
