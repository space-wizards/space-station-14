using Content.Shared.Chemistry.Components;
using Content.Shared.Inventory;

namespace Content.Shared.Chemistry;

public sealed class SolutionScannerSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<SolutionScannerComponent, SolutionScanEvent>(OnSolutionScanAttempt);
        SubscribeLocalEvent<SolutionScannerComponent, InventoryRelayedEvent<SolutionScanEvent>>((e, c, ev) => OnSolutionScanAttempt(e, c, ev.Args));
    }

    private void OnSolutionScanAttempt(EntityUid eid, SolutionScannerComponent component, SolutionScanEvent args)
    {
        args.CanScan = true;
    }
}

public sealed class SolutionScanEvent : EntityEventArgs, IInventoryRelayEvent
{
    public bool CanScan;
    public SlotFlags TargetSlots { get; } = SlotFlags.EYES;
}
