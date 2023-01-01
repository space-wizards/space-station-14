using Content.Server.Anomaly.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Anomaly;
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
    public void InitializeVessel()
    {
        SubscribeLocalEvent<AnomalyVesselComponent, ComponentShutdown>(OnVesselShutdown);
        SubscribeLocalEvent<AnomalyVesselComponent, MapInitEvent>(OnVesselMapInit);
        SubscribeLocalEvent<AnomalyVesselComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<AnomalyVesselComponent, ResearchServerGetPointsPerSecondEvent>(OnGetPointsPerSecond);
        SubscribeLocalEvent<AnomalyVesselComponent, AnomalyShutdownEvent>(OnAnomalyShutdown);
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

    private void OnInteractUsing(EntityUid uid, AnomalyVesselComponent component, InteractUsingEvent args)
    {
        if (component.Anomaly != null ||
            !TryComp<AnomalyScannerComponent>(args.Used, out var scanner) ||
            scanner.ScannedAnomaly is not {} anomaly)
        {
            return;
        }

        if (!TryComp<AnomalyComponent>(anomaly, out var anomalyComponent) || anomalyComponent.ConnectedVessel != null)
            return;

        UpdateVesselAppearance(uid,  component);
        component.Anomaly = scanner.ScannedAnomaly;
        anomalyComponent.ConnectedVessel = uid;
        _popup.PopupEntity(Loc.GetString("anomaly-vessel-component-anomaly-assigned"), uid);
    }

    private void OnGetPointsPerSecond(EntityUid uid, AnomalyVesselComponent component, ref ResearchServerGetPointsPerSecondEvent args)
    {
        if (!this.IsPowered(uid, EntityManager) || component.Anomaly is not {} anomaly)
        {
            args.Points = 0;
            return;
        }

        args.Points = GetAnomalyPointValue(anomaly);
    }

    private void OnAnomalyShutdown(EntityUid uid, AnomalyVesselComponent component, ref AnomalyShutdownEvent args)
    {
        if (args.Anomaly != component.Anomaly)
            return;

        UpdateVesselAppearance(uid,  component);
        component.Anomaly = null;

        if (!args.Supercritical)
            return;
        _explosion.TriggerExplosive(uid);
    }

    /// <summary>
    /// Gets the amount of research points generated per second for an anomaly.
    /// </summary>
    /// <param name="anomaly"></param>
    /// <param name="component"></param>
    /// <returns>The amount of points</returns>
    public int GetAnomalyPointValue(EntityUid anomaly, AnomalyComponent? component = null)
    {
        if (!Resolve(anomaly, ref component))
            return 0;

        var multiplier = 1f;
        if (component.Stability > component.GrowthThreshold)
            multiplier = 1.25f; //more points for unstable
        else if (component.Stability < component.DeathThreshold)
            multiplier = 0.75f; //less points if it's dying

        //penalty of up to 50% based on health
        multiplier *= MathF.Pow(1.5f, component.Health) - 0.5f;

        return (int) ((component.MaxPointsPerSecond - component.MinPointsPerSecond) * component.Severity * multiplier);
    }

    public void UpdateVesselAppearance(EntityUid uid, AnomalyVesselComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var on = component.Anomaly != null;

        _appearance.SetData(uid, AnomalyVesselVisuals.HasAnomaly, on);
        if (TryComp<SharedPointLightComponent>(uid, out var pointLightComponent))
        {
            pointLightComponent.Enabled = on;
        }
    }
}
