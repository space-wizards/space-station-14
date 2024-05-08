using Content.Shared.Inventory;
using Content.Shared.Silicons.Laws.Components;

public sealed class DiagnosticsScannerSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<DiagnosticScannerComponent, DiagnosticScanEvent>(OnDiagnosticsScanAttempt);
        SubscribeLocalEvent<DiagnosticScannerComponent, InventoryRelayedEvent<DiagnosticScanEvent>>((e, c, ev) => OnDiagnosticsScanAttempt(e, c, ev.Args));
    }

    private void OnDiagnosticsScanAttempt(EntityUid eid, DiagnosticScannerComponent component, DiagnosticScanEvent args)
    {
        args.CanScan = true;
    }

}

public sealed class DiagnosticScanEvent : EntityEventArgs, IInventoryRelayEvent
{
    public bool CanScan;
    public SlotFlags TargetSlots { get; } = SlotFlags.EYES;
}