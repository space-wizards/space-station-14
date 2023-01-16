using Content.Server.Anomaly.Components;
using Content.Server.Construction;
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
        SubscribeLocalEvent<AnomalyVesselComponent, RefreshPartsEvent>(OnRefreshParts);
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

    private void OnRefreshParts(EntityUid uid, AnomalyVesselComponent component, RefreshPartsEvent args)
    {
        var modifierRating = args.PartRatings[component.MachinePartPointModifier] - 1;
        component.PointMultiplier = MathF.Pow(component.PartRatingPointModifier, modifierRating);
    }

    private void OnVesselInteractUsing(EntityUid uid, AnomalyVesselComponent component, InteractUsingEvent args)
    {
        if (component.Anomaly != null ||
            !TryComp<AnomalyScannerComponent>(args.Used, out var scanner) ||
            scanner.ScannedAnomaly is not {} anomaly)
        {
            return;
        }

        if (!TryComp<AnomalyComponent>(anomaly, out var anomalyComponent) || anomalyComponent.ConnectedVessel != null)
            return;

        component.Anomaly = scanner.ScannedAnomaly;
        anomalyComponent.ConnectedVessel = uid;
        UpdateVesselAppearance(uid,  component);
        _popup.PopupEntity(Loc.GetString("anomaly-vessel-component-anomaly-assigned"), uid);
    }

    private void OnVesselGetPointsPerSecond(EntityUid uid, AnomalyVesselComponent component, ref ResearchServerGetPointsPerSecondEvent args)
    {
        if (!this.IsPowered(uid, EntityManager) || component.Anomaly is not {} anomaly)
        {
            args.Points = 0;
            return;
        }

        args.Points += (int) (GetAnomalyPointValue(anomaly) * component.PointMultiplier);
    }

    private void OnVesselAnomalyShutdown(ref AnomalyShutdownEvent args)
    {
        foreach (var component in EntityQuery<AnomalyVesselComponent>())
        {
            var ent = component.Owner;

            if (args.Anomaly != component.Anomaly)
                continue;

            component.Anomaly = null;
            UpdateVesselAppearance(ent,  component);

            if (!args.Supercritical)
                continue;
            _explosion.TriggerExplosive(ent);
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

        Appearance.SetData(uid, AnomalyVesselVisuals.HasAnomaly, on);
        if (TryComp<SharedPointLightComponent>(uid, out var pointLightComponent))
        {
            pointLightComponent.Enabled = on;
        }
        _ambient.SetAmbience(uid, on);
    }
}
