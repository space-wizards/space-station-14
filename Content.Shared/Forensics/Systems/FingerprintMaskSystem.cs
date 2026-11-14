using Content.Shared.Forensics.Components;
using Content.Shared.Inventory;

namespace Content.Shared.Forensics.Systems;

public sealed partial class FingerprintMaskSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnTryAccessFingerprint(Entity<FingerprintMaskComponent> gloves, ref InventoryRelayedEvent<TryAccessFingerprintEvent> args)
    {
        if (args.Args.Cancelled)
            return;

        args.Args.Blocker = gloves.Owner;
        args.Args.Cancel();
    }
}
