using Content.Server.Anomaly.Components;
using Content.Server.Anomaly.Effects;
using Content.Shared.Anomaly;
using Content.Shared.Anomaly.Components;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;

namespace Content.Server.Anomaly;

public sealed class AnomalyScannerSystem : SharedAnomalyScannerSystem
{

    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SecretDataAnomalySystem _secretData = default!;
    [Dependency] private readonly AnomalySystem _anomaly = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AnomalyScannerComponent, BoundUIOpenedEvent>(OnScannerUiOpened);
        SubscribeLocalEvent<AnomalyScannerComponent, AfterInteractEvent>(OnScannerAfterInteract);
        SubscribeLocalEvent<AnomalyScannerComponent, ScannerDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<AnomalyShutdownEvent>(OnScannerAnomalyShutdown);
        SubscribeLocalEvent<AnomalySeverityChangedEvent>(OnScannerAnomalySeverityChanged);
        SubscribeLocalEvent<AnomalyStabilityChangedEvent>(OnScannerAnomalyStabilityChanged);
        SubscribeLocalEvent<AnomalyHealthChangedEvent>(OnScannerAnomalyHealthChanged);
        SubscribeLocalEvent<AnomalyBehaviorChangedEvent>(OnScannerAnomalyBehaviorChanged);

    }

    private void OnScannerUiOpened(EntityUid uid, AnomalyScannerComponent component, BoundUIOpenedEvent args)
    {
        UpdateScannerUi(uid, component);
    }

    private void OnScannerAfterInteract(EntityUid uid, AnomalyScannerComponent component, AfterInteractEvent args)
    {
        if (args.Target is not { } target)
            return;
        if (!HasComp<AnomalyComponent>(target))
            return;
        if (!args.CanReach)
            return;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, component.ScanDoAfterDuration, new ScannerDoAfterEvent(), uid, target: target, used: uid)
        {
            DistanceThreshold = 2f
        });
    }

    public void UpdateScannerWithNewAnomaly(EntityUid scanner, EntityUid anomaly, AnomalyScannerComponent? scannerComp = null, AnomalyComponent? anomalyComp = null)
    {
        if (!Resolve(scanner, ref scannerComp) || !Resolve(anomaly, ref anomalyComp))
            return;

        scannerComp.ScannedAnomaly = anomaly;
        UpdateScannerUi(scanner, scannerComp);
        AppearanceComponent? appearanceComp = null;
        _appearance.SetData(scanner, AnomalyScannerVisuals.HasAnomaly, true, appearanceComp);
        var stability = _secretData.IsSecret(anomaly, AnomalySecretData.Stability)
            ? AnomalyStabilityVisuals.Stable
            : _anomaly.GetStabilityVisualOrStable((anomaly, anomalyComp));
        _appearance.SetData(scanner, AnomalyScannerVisuals.AnomalyStability, stability, appearanceComp);
        var severity = _secretData.IsSecret(anomaly, AnomalySecretData.Severity) ? 0 : anomalyComp.Severity;
        _appearance.SetData(scanner, AnomalyScannerVisuals.AnomalySeverity, severity, appearanceComp);
    }

    private void OnDoAfter(EntityUid uid, AnomalyScannerComponent component, DoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;


        Audio.PlayPvs(component.CompleteSound, uid);
        Popup.PopupEntity(Loc.GetString("anomaly-scanner-component-scan-complete"), uid);
        UpdateScannerWithNewAnomaly(uid, args.Args.Target.Value, component);

        _ui.OpenUi(uid, AnomalyScannerUiKey.Key, args.User);

        args.Handled = true;
    }

    public void UpdateScannerUi(EntityUid uid, AnomalyScannerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        TimeSpan? nextPulse = null;
        if (TryComp<AnomalyComponent>(component.ScannedAnomaly, out var anomalyComponent))
            nextPulse = anomalyComponent.NextPulseTime;

        var state = new AnomalyScannerUserInterfaceState(_anomaly.GetScannerMessage(component), nextPulse);
        _ui.SetUiState(uid, AnomalyScannerUiKey.Key, state);
    }

    private void OnScannerAnomalyShutdown(ref AnomalyShutdownEvent args)
    {
        var query = EntityQueryEnumerator<AnomalyScannerComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.ScannedAnomaly != args.Anomaly)
                continue;

            _ui.CloseUi(uid, AnomalyScannerUiKey.Key);
            // Anomaly over, reset all the appearance data
            _appearance.SetData(uid, AnomalyScannerVisuals.HasAnomaly, false);
            _appearance.SetData(uid, AnomalyScannerVisuals.AnomalyIsSupercritical, false);
            _appearance.SetData(uid, AnomalyScannerVisuals.AnomalyNextPulse, 0);
            _appearance.SetData(uid, AnomalyScannerVisuals.AnomalySeverity, 0);
            _appearance.SetData(uid, AnomalyScannerVisuals.AnomalyStability, AnomalyStabilityVisuals.Stable);
        }
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
            _appearance.SetData(uid, AnomalyScannerVisuals.AnomalySeverity, severity);
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
            _appearance.SetData(uid, AnomalyScannerVisuals.AnomalyStability, stability);
        }
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
            TryComp<SecretDataAnomalyComponent>(args.Anomaly, out var secretDataComp);
            var severity = _secretData.IsSecret(args.Anomaly, AnomalySecretData.Severity, secretDataComp)
                ? 0
                : anomalyComp.Severity;
            _appearance.SetData(uid, AnomalyScannerVisuals.AnomalySeverity, severity);
            var stability = _secretData.IsSecret(args.Anomaly, AnomalySecretData.Stability, secretDataComp)
                ? AnomalyStabilityVisuals.Stable
                : _anomaly.GetStabilityVisualOrStable((args.Anomaly, anomalyComp));
            _appearance.SetData(uid, AnomalyScannerVisuals.AnomalyStability, stability);
        }
    }

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
