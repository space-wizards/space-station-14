using Content.Server.Anomaly.Components;
using Content.Server.Anomaly.Effects;
using Content.Shared.Anomaly;
using Content.Shared.Anomaly.Components;
using Content.Shared.DoAfter;

namespace Content.Server.Anomaly;

/// <inheritdoc cref="SharedAnomalyScannerSystem"/>
public sealed class AnomalyScannerSystem : SharedAnomalyScannerSystem
{
    [Dependency] private readonly SecretDataAnomalySystem _secretData = default!;
    [Dependency] private readonly AnomalySystem _anomaly = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnomalySeverityChangedEvent>(OnScannerAnomalySeverityChanged);
        SubscribeLocalEvent<AnomalyStabilityChangedEvent>(OnScannerAnomalyStabilityChanged);
        SubscribeLocalEvent<AnomalyHealthChangedEvent>(OnScannerAnomalyHealthChanged);
        SubscribeLocalEvent<AnomalyBehaviorChangedEvent>(OnScannerAnomalyBehaviorChanged);

        Subs.BuiEvents<AnomalyScannerComponent>(
            AnomalyScannerUiKey.Key,
            subs => subs.Event<BoundUIOpenedEvent>(OnScannerUiOpened)
        );
    }

    /// <summary> Updates device with passed anomaly data. </summary>
    public void UpdateScannerWithNewAnomaly(EntityUid scanner, EntityUid anomaly, AnomalyScannerComponent? scannerComp = null, AnomalyComponent? anomalyComp = null)
    {
        if (!Resolve(scanner, ref scannerComp) || !Resolve(anomaly, ref anomalyComp))
            return;

        scannerComp.ScannedAnomaly = anomaly;
        UpdateScannerUi(scanner, scannerComp);

        TryComp<AppearanceComponent>(scanner, out var appearanceComp);
        TryComp<SecretDataAnomalyComponent>(anomaly, out var secretDataComp);

        Appearance.SetData(scanner, AnomalyScannerVisuals.HasAnomaly, true, appearanceComp);

        var stability = _secretData.IsSecret(anomaly, AnomalySecretData.Stability, secretDataComp)
            ? AnomalyStabilityVisuals.Stable
            : _anomaly.GetStabilityVisualOrStable((anomaly, anomalyComp));
        Appearance.SetData(scanner, AnomalyScannerVisuals.AnomalyStability, stability, appearanceComp);

        var severity = _secretData.IsSecret(anomaly, AnomalySecretData.Severity, secretDataComp)
            ? 0
            : anomalyComp.Severity;
        Appearance.SetData(scanner, AnomalyScannerVisuals.AnomalySeverity, severity, appearanceComp);
    }

    /// <summary> Update scanner interface. </summary>
    public void UpdateScannerUi(EntityUid uid, AnomalyScannerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        TimeSpan? nextPulse = null;
        if (TryComp<AnomalyComponent>(component.ScannedAnomaly, out var anomalyComponent))
            nextPulse = anomalyComponent.NextPulseTime;

        var state = new AnomalyScannerUserInterfaceState(_anomaly.GetScannerMessage(component), nextPulse);
        UI.SetUiState(uid, AnomalyScannerUiKey.Key, state);
    }

    /// <inheritdoc />
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var anomalyQuery = EntityQueryEnumerator<AnomalyComponent>();
        while (anomalyQuery.MoveNext(out var ent, out var anomaly))
        {
            var secondsUntilNextPulse = (anomaly.NextPulseTime - Timing.CurTime).TotalSeconds;
            UpdateScannerPulseTimers((ent, anomaly),  secondsUntilNextPulse);
        }
    }

    /// <inheritdoc />
    protected override void OnDoAfter(EntityUid uid, AnomalyScannerComponent component, DoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        base.OnDoAfter(uid, component, args);

        UpdateScannerWithNewAnomaly(uid, args.Args.Target.Value, component);
    }

    private void OnScannerAnomalyHealthChanged(ref AnomalyHealthChangedEvent args)
    {
        var query = EntityQueryEnumerator<AnomalyScannerComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.ScannedAnomaly != args.Anomaly)
                continue;

            UpdateScannerUi(uid, component);
        }
    }

    private void OnScannerUiOpened(EntityUid uid, AnomalyScannerComponent component, BoundUIOpenedEvent args)
    {
        UpdateScannerUi(uid, component);
    }

    private void OnScannerAnomalySeverityChanged(ref AnomalySeverityChangedEvent args)
    {
        var severity = _secretData.IsSecret(args.Anomaly, AnomalySecretData.Severity) ? 0 : args.Severity;
        var query = EntityQueryEnumerator<AnomalyScannerComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.ScannedAnomaly != args.Anomaly)
                continue;

            UpdateScannerUi(uid, component);
            Appearance.SetData(uid, AnomalyScannerVisuals.AnomalySeverity, severity);
        }
    }

    private void OnScannerAnomalyStabilityChanged(ref AnomalyStabilityChangedEvent args)
    {
        var stability = _secretData.IsSecret(args.Anomaly, AnomalySecretData.Stability)
            ? AnomalyStabilityVisuals.Stable
            : _anomaly.GetStabilityVisualOrStable(args.Anomaly);
        var query = EntityQueryEnumerator<AnomalyScannerComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.ScannedAnomaly != args.Anomaly)
                continue;

            UpdateScannerUi(uid, component);
            Appearance.SetData(uid, AnomalyScannerVisuals.AnomalyStability, stability);
        }
    }

    private void OnScannerAnomalyBehaviorChanged(ref AnomalyBehaviorChangedEvent args)
    {
        var query = EntityQueryEnumerator<AnomalyScannerComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.ScannedAnomaly != args.Anomaly)
                continue;

            UpdateScannerUi(uid, component);
            // If a field becomes secret, we want to set it to 0 or stable
            // If a field becomes visible, we need to set it to the correct value, so we need to get the AnomalyComponent
            if (!TryComp<AnomalyComponent>(args.Anomaly, out var anomalyComp))
                return;

            TryComp<AppearanceComponent>(uid, out var appearanceComp);
            TryComp<SecretDataAnomalyComponent>(args.Anomaly, out var secretDataComp);

            var severity = _secretData.IsSecret(args.Anomaly, AnomalySecretData.Severity, secretDataComp)
                ? 0
                : anomalyComp.Severity;
            Appearance.SetData(uid, AnomalyScannerVisuals.AnomalySeverity, severity, appearanceComp);

            var stability = _secretData.IsSecret(args.Anomaly, AnomalySecretData.Stability, secretDataComp)
                ? AnomalyStabilityVisuals.Stable
                : _anomaly.GetStabilityVisualOrStable((args.Anomaly, anomalyComp));
            Appearance.SetData(uid, AnomalyScannerVisuals.AnomalyStability, stability, appearanceComp);
        }
    }

    private void UpdateScannerPulseTimers(Entity<AnomalyComponent> anomalyEnt, double secondsUntilNextPulse)
    {
        if (secondsUntilNextPulse > 5)
            return;

        var rounded = Math.Max(0, (int)Math.Ceiling(secondsUntilNextPulse));

        var scannerQuery = EntityQueryEnumerator<AnomalyScannerComponent>();
        while (scannerQuery.MoveNext(out var scannerUid, out var scanner))
        {
            if (scanner.ScannedAnomaly != anomalyEnt)
                continue;

            Appearance.SetData(scannerUid, AnomalyScannerVisuals.AnomalyNextPulse, rounded);
        }
    }
}
