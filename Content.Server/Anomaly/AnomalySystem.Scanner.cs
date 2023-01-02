using Content.Server.Anomaly.Components;
using Content.Server.DoAfter;
using Content.Shared.Anomaly;
using Content.Shared.Interaction;

namespace Content.Server.Anomaly;

/// <summary>
/// This handles the anomaly scanner and it's UI updates.
/// </summary>
public sealed partial class AnomalySystem
{
    public void InitializeScanner()
    {
        SubscribeLocalEvent<AnomalyScannerComponent, AfterInteractEvent>(OnScannerAfterInteract);
        SubscribeLocalEvent<AnomalyScannerComponent, AnomalyScanFinishedEvent>(OnScannerDoAfterFinished);
        SubscribeLocalEvent<AnomalyScannerComponent, AnomalyScanCancelledEvent>(OnScannerDoAfterCancelled);
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

        component.ScannedAnomaly = args.Anomaly;
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
    }
}
