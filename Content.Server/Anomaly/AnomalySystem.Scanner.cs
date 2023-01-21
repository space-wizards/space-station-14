using Content.Server.Anomaly.Components;
using Content.Server.DoAfter;
using Content.Shared.Anomaly;
using Content.Shared.Anomaly.Components;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.Utility;

namespace Content.Server.Anomaly;

/// <summary>
/// This handles the anomaly scanner and it's UI updates.
/// </summary>
public sealed partial class AnomalySystem
{
    private void InitializeScanner()
    {
        SubscribeLocalEvent<AnomalyScannerComponent, BoundUIOpenedEvent>(OnScannerUiOpened);
        SubscribeLocalEvent<AnomalyScannerComponent, AfterInteractEvent>(OnScannerAfterInteract);
        SubscribeLocalEvent<AnomalyScannerComponent, AnomalyScanFinishedEvent>(OnScannerDoAfterFinished);
        SubscribeLocalEvent<AnomalyScannerComponent, AnomalyScanCancelledEvent>(OnScannerDoAfterCancelled);

        SubscribeLocalEvent<AnomalyShutdownEvent>(OnScannerAnomalyShutdown);
        SubscribeLocalEvent<AnomalySeverityChangedEvent>(OnScannerAnomalySeverityChanged);
        SubscribeLocalEvent<AnomalyStabilityChangedEvent>(OnScannerAnomalyStabilityChanged);
        SubscribeLocalEvent<AnomalyHealthChangedEvent>(OnScannerAnomalyHealthChanged);
    }

    private void OnScannerAnomalyShutdown(ref AnomalyShutdownEvent args)
    {
        foreach (var component in EntityQuery<AnomalyScannerComponent>())
        {
            if (component.ScannedAnomaly != args.Anomaly)
                continue;
            _ui.TryCloseAll(component.Owner, AnomalyScannerUiKey.Key);
        }
    }

    private void OnScannerAnomalySeverityChanged(ref AnomalySeverityChangedEvent args)
    {
        foreach (var component in EntityQuery<AnomalyScannerComponent>())
        {
            if (component.ScannedAnomaly != args.Anomaly)
                continue;
            UpdateScannerUi(component.Owner, component);
        }
    }

    private void OnScannerAnomalyStabilityChanged(ref AnomalyStabilityChangedEvent args)
    {
        foreach (var component in EntityQuery<AnomalyScannerComponent>())
        {
            if (component.ScannedAnomaly != args.Anomaly)
                continue;
            UpdateScannerUi(component.Owner, component);
        }
    }

    private void OnScannerAnomalyHealthChanged(ref AnomalyHealthChangedEvent args)
    {
        foreach (var component in EntityQuery<AnomalyScannerComponent>())
        {
            if (component.ScannedAnomaly != args.Anomaly)
                continue;
            UpdateScannerUi(component.Owner, component);
        }
    }

    private void OnScannerUiOpened(EntityUid uid, AnomalyScannerComponent component, BoundUIOpenedEvent args)
    {
        UpdateScannerUi(uid, component);
    }

    private void OnScannerAfterInteract(EntityUid uid, AnomalyScannerComponent component, AfterInteractEvent args)
    {
        if (component.TokenSource != null)
            return;

        if (args.Target is not { } target)
            return;
        if (!HasComp<AnomalyComponent>(target))
            return;

        component.TokenSource = new();
        _doAfter.DoAfter(new DoAfterEventArgs(args.User, component.ScanDoAfterDuration, component.TokenSource.Token, target, uid)
        {
            DistanceThreshold = 2f,
            UsedFinishedEvent = new AnomalyScanFinishedEvent(target, args.User),
            UsedCancelledEvent = new AnomalyScanCancelledEvent()
        });
    }

    private void OnScannerDoAfterFinished(EntityUid uid, AnomalyScannerComponent component, AnomalyScanFinishedEvent args)
    {
        component.TokenSource = null;

        Audio.PlayPvs(component.CompleteSound, uid);
        Popup.PopupEntity(Loc.GetString("anomaly-scanner-component-scan-complete"), uid);
        UpdateScannerWithNewAnomaly(uid, args.Anomaly, component);

        if (TryComp<ActorComponent>(args.User, out var actor))
            _ui.TryOpen(uid, AnomalyScannerUiKey.Key, actor.PlayerSession);
    }

    private void OnScannerDoAfterCancelled(EntityUid uid, AnomalyScannerComponent component, AnomalyScanCancelledEvent args)
    {
        component.TokenSource = null;
    }

    public void UpdateScannerUi(EntityUid uid, AnomalyScannerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        TimeSpan? nextPulse = null;
        if (TryComp<AnomalyComponent>(component.ScannedAnomaly, out var anomalyComponent))
            nextPulse = anomalyComponent.NextPulseTime;

        var state = new AnomalyScannerUserInterfaceState(GetScannerMessage(component), nextPulse);
        _ui.TrySetUiState(uid, AnomalyScannerUiKey.Key, state);
    }

    public void UpdateScannerWithNewAnomaly(EntityUid scanner, EntityUid anomaly, AnomalyScannerComponent? scannerComp = null, AnomalyComponent? anomalyComp = null)
    {
        if (!Resolve(scanner, ref scannerComp) || !Resolve(anomaly, ref anomalyComp))
            return;

        scannerComp.ScannedAnomaly = anomaly;
        UpdateScannerUi(scanner, scannerComp);
    }

    public FormattedMessage GetScannerMessage(AnomalyScannerComponent component)
    {
        var msg = new FormattedMessage();
        if (component.ScannedAnomaly is not { } anomaly || !TryComp<AnomalyComponent>(anomaly, out var anomalyComp))
        {
            msg.AddMarkup(Loc.GetString("anomaly-scanner-no-anomaly"));
            return msg;
        }

        msg.AddMarkup(Loc.GetString("anomaly-scanner-severity-percentage", ("percent", anomalyComp.Severity.ToString("P"))));
        msg.PushNewline();
        string stateLoc;
        if (anomalyComp.Stability < anomalyComp.DecayThreshold)
            stateLoc = Loc.GetString("anomaly-scanner-stability-low");
        else if (anomalyComp.Stability > anomalyComp.GrowthThreshold)
            stateLoc =  Loc.GetString("anomaly-scanner-stability-high");
        else
            stateLoc =  Loc.GetString("anomaly-scanner-stability-medium");
        msg.AddMarkup(stateLoc);
        msg.PushNewline();

        var points = GetAnomalyPointValue(anomaly, anomalyComp) / 10 * 10; //round to tens place
        msg.AddMarkup(Loc.GetString("anomaly-scanner-point-output", ("point", points)));
        msg.PushNewline();
        msg.PushNewline();

        msg.AddMarkup(Loc.GetString("anomaly-scanner-particle-readout"));
        msg.PushNewline();
        msg.AddMarkup(Loc.GetString("anomaly-scanner-particle-danger", ("type", GetParticleLocale(anomalyComp.SeverityParticleType))));
        msg.PushNewline();
        msg.AddMarkup(Loc.GetString("anomaly-scanner-particle-unstable", ("type", GetParticleLocale(anomalyComp.DestabilizingParticleType))));
        msg.PushNewline();
        msg.AddMarkup(Loc.GetString("anomaly-scanner-particle-containment", ("type", GetParticleLocale(anomalyComp.WeakeningParticleType))));

        //The timer at the end here is actually added in the ui itself.
        return msg;
    }
}
