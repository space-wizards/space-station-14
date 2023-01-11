using Content.Server.Anomaly.Components;
using Content.Server.DoAfter;
using Content.Shared.Anomaly;
using Content.Shared.Interaction;
using Robust.Shared.Utility;

namespace Content.Server.Anomaly;

/// <summary>
/// This handles the anomaly scanner and it's UI updates.
/// </summary>
public sealed partial class AnomalySystem
{
    public void InitializeScanner()
    {
        SubscribeLocalEvent<AnomalyScannerComponent, BoundUIOpenedEvent>(OnScannerUiOpened);
        SubscribeLocalEvent<AnomalyScannerComponent, AfterInteractEvent>(OnScannerAfterInteract);
        SubscribeLocalEvent<AnomalyScannerComponent, AnomalyScanFinishedEvent>(OnScannerDoAfterFinished);
        SubscribeLocalEvent<AnomalyScannerComponent, AnomalyScanCancelledEvent>(OnScannerDoAfterCancelled);
    }

    private void OnScannerUiOpened(EntityUid uid, AnomalyScannerComponent component, BoundUIOpenedEvent args)
    {
        var state = new AnomalyScannerUserInterfaceState(GetScannerMessage(component));
        _ui.TrySetUiState(uid, AnomalyScannerUiKey.Key, state);
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
            DistanceThreshold = 1.5f,
            UsedFinishedEvent = new AnomalyScanFinishedEvent(target),
            UsedCancelledEvent = new AnomalyScanCancelledEvent()
        });
    }

    private void OnScannerDoAfterFinished(EntityUid uid, AnomalyScannerComponent component, AnomalyScanFinishedEvent args)
    {
        component.TokenSource = null;

        _popup.PopupEntity(Loc.GetString("anomaly-scanner-component-scan-complete"), uid);
        UpdateScannerWithNewAnomaly(uid, args.Anomaly, component);
    }

    private void OnScannerDoAfterCancelled(EntityUid uid, AnomalyScannerComponent component, AnomalyScanCancelledEvent args)
    {
        component.TokenSource = null;
    }

    public void UpdateScannerWithNewAnomaly(EntityUid scanner, EntityUid anomaly, AnomalyScannerComponent? scannerComp = null, AnomalyComponent? anomalyComp = null)
    {
        if (!Resolve(scanner, ref scannerComp) || !Resolve(anomaly, ref anomalyComp))
            return;

        scannerComp.ScannedAnomaly = anomaly;
    }

    public FormattedMessage GetScannerMessage(AnomalyScannerComponent component)
    {
        var msg = new FormattedMessage();
        if (component.ScannedAnomaly is not { } anomaly || !TryComp<AnomalyComponent>(anomaly, out var anomalyComp))
        {
            msg.AddMarkup(Loc.GetString("anomaly-scanner-no-anomaly"));
            return msg;
        }

        string stateLoc;
        if (anomalyComp.Stability < anomalyComp.DecayThreshold)
            stateLoc = Loc.GetString("anomaly-scanner-stability-low");
        else if (anomalyComp.Stability > anomalyComp.GrowthThreshold)
            stateLoc =  Loc.GetString("anomaly-scanner-stability-high");
        else
            stateLoc =  Loc.GetString("anomaly-scanner-stability-medium");
        msg.AddMarkup(stateLoc);
        msg.PushNewline();

        msg.AddMarkup(Loc.GetString("anomaly-scanner-pulse-timer", ("time", anomalyComp.NextPulseTime - _timing.CurTime)));
        msg.PushNewline();
        msg.PushNewline();

        msg.AddMarkup(Loc.GetString("anomaly-scanner-particle-readout"));
        msg.PushNewline();
        msg.AddMarkup(Loc.GetString("anomaly-scanner-particle-danger", ("type", GetParticleLocale(anomalyComp.SeverityParticleType))));
        msg.PushNewline();
        msg.AddMarkup(Loc.GetString("anomaly-scanner-particle-unstable", ("type", GetParticleLocale(anomalyComp.DestabilizingParticleType))));
        msg.PushNewline();
        msg.AddMarkup(Loc.GetString("anomaly-scanner-particle-containment", ("type", GetParticleLocale(anomalyComp.WeakeningParticleType))));

        return msg;
    }
}
