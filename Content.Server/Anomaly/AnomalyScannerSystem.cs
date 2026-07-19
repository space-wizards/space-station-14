using Content.Server.Anomaly.Components;
using Content.Server.Anomaly.Effects;
using Content.Shared.Anomaly;
using Content.Shared.Anomaly.Components;
using Content.Shared.DoAfter;
using Robust.Shared.Timing;

namespace Content.Server.Anomaly;

/// <inheritdoc cref="SharedAnomalyScannerSystem"/>
public sealed partial class AnomalyScannerSystem : SharedAnomalyScannerSystem
{
    private static readonly EntityTimerId PulseCountdownTimer = new("pulse-countdown");
    private static readonly TimeSpan PulseCountdownLength = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan PulseCountdownInterval = TimeSpan.FromSeconds(1);

    [Dependency] private SecretDataAnomalySystem _secretData = default!;
    [Dependency] private AnomalySystem _anomaly = default!;
    [Dependency] private IEntityTimerManager _timers = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnomalySeverityChangedEvent>(OnScannerAnomalySeverityChanged);
        SubscribeLocalEvent<AnomalyStabilityChangedEvent>(OnScannerAnomalyStabilityChanged);
        SubscribeLocalEvent<AnomalyHealthChangedEvent>(OnScannerAnomalyHealthChanged);
        SubscribeLocalEvent<AnomalyBehaviorChangedEvent>(OnScannerAnomalyBehaviorChanged);
        SubscribeLocalEvent<AnomalyComponent, AnomalyPulseEvent>(OnAnomalyPulse);
        SubscribeLocalEvent<AnomalyScannerComponent, EntityTimerEvent>(OnPulseCountdownTimer);

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
        SchedulePulseCountdown((scanner, scannerComp), anomalyComp);
        UpdateScannerUi(scanner, scannerComp);

        TryComp<AppearanceComponent>(scanner, out var appearanceComp);
        TryComp<SecretDataAnomalyComponent>(anomaly, out var secretDataComp);

        Appearance.SetData(scanner, AnomalyScannerVisuals.HasAnomaly, true, appearanceComp);

        var stability = _secretData.IsSecret(anomaly, AnomalySecretData.Stability, secretDataComp) && !scannerComp.IgnoreSecret
            ? AnomalyStabilityVisuals.Stable
            : _anomaly.GetStabilityVisualOrStable((anomaly, anomalyComp));
        Appearance.SetData(scanner, AnomalyScannerVisuals.AnomalyStability, stability, appearanceComp);

        var severity = _secretData.IsSecret(anomaly, AnomalySecretData.Severity, secretDataComp) && !scannerComp.IgnoreSecret
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

    private void OnAnomalyPulse(Entity<AnomalyComponent> ent, ref AnomalyPulseEvent args)
    {
        var scannerQuery = EntityQueryEnumerator<AnomalyScannerComponent>();
        while (scannerQuery.MoveNext(out var scannerUid, out var scanner))
        {
            if (scanner.ScannedAnomaly == ent.Owner)
                SchedulePulseCountdown((scannerUid, scanner), ent.Comp);
        }
    }

    private void OnPulseCountdownTimer(Entity<AnomalyScannerComponent> scanner, ref EntityTimerEvent args)
    {
        if (args.Id != PulseCountdownTimer ||
            !TryComp<AnomalyComponent>(scanner.Comp.ScannedAnomaly, out var anomaly))
            return;

        var secondsUntilNextPulse = (anomaly.NextPulseTime - args.FiredAt).TotalSeconds;
        if (secondsUntilNextPulse > PulseCountdownLength.TotalSeconds)
        {
            _timers.CancelTimer<AnomalyScannerComponent>(scanner, PulseCountdownTimer);
            return;
        }

        UpdateScannerPulseTimer(scanner, secondsUntilNextPulse);
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

        if (TryComp<AnomalyComponent>(component.ScannedAnomaly, out var anomaly))
            SchedulePulseCountdown((uid, component), anomaly);
    }

    private void OnScannerAnomalySeverityChanged(ref AnomalySeverityChangedEvent args)
    {
        var query = EntityQueryEnumerator<AnomalyScannerComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.ScannedAnomaly != args.Anomaly)
                continue;

            var severity = _secretData.IsSecret(args.Anomaly, AnomalySecretData.Severity) && !component.IgnoreSecret ? 0 : args.Severity;

            UpdateScannerUi(uid, component);
            Appearance.SetData(uid, AnomalyScannerVisuals.AnomalySeverity, severity);
        }
    }

    private void OnScannerAnomalyStabilityChanged(ref AnomalyStabilityChangedEvent args)
    {
        var query = EntityQueryEnumerator<AnomalyScannerComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.ScannedAnomaly != args.Anomaly)
                continue;

            var stability = _secretData.IsSecret(args.Anomaly, AnomalySecretData.Stability) && !component.IgnoreSecret
                ? AnomalyStabilityVisuals.Stable
                : _anomaly.GetStabilityVisualOrStable(args.Anomaly);

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

            var severity = _secretData.IsSecret(args.Anomaly, AnomalySecretData.Severity, secretDataComp) && !component.IgnoreSecret
                ? 0
                : anomalyComp.Severity;
            Appearance.SetData(uid, AnomalyScannerVisuals.AnomalySeverity, severity, appearanceComp);

            var stability = _secretData.IsSecret(args.Anomaly, AnomalySecretData.Stability, secretDataComp) && !component.IgnoreSecret
                ? AnomalyStabilityVisuals.Stable
                : _anomaly.GetStabilityVisualOrStable((args.Anomaly, anomalyComp));
            Appearance.SetData(uid, AnomalyScannerVisuals.AnomalyStability, stability, appearanceComp);
        }
    }

    private void UpdateScannerPulseTimer(Entity<AnomalyScannerComponent> scanner, double secondsUntilNextPulse)
    {
        if (secondsUntilNextPulse > 5)
            return;

        var rounded = Math.Max(0, (int)Math.Ceiling(secondsUntilNextPulse));
        Appearance.SetData(scanner, AnomalyScannerVisuals.AnomalyNextPulse, rounded);
    }

    private void SchedulePulseCountdown(Entity<AnomalyScannerComponent> scanner, AnomalyComponent anomaly)
    {
        var deadline = anomaly.NextPulseTime - PulseCountdownLength;
        _timers.SetTimerAt(scanner, PulseCountdownTimer, deadline, PulseCountdownInterval);
    }
}
