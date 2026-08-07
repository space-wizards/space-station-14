using Content.Shared.Forensics.Components;
using Content.Shared.Forensics.Events;
using Content.Shared.Inventory;

namespace Content.Shared.Forensics.Systems;

public sealed class FingerprintMaskSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnTryAccessFingerprint(Entity<FingerprintMaskComponent> gloves, ref InventoryRelayedEvent<TryAccessFingerprintEvent> args)
    {
        if (args.Args.Blocker.HasValue)
            return;

        args.Args.Blocker = gloves.Owner;
    }
}
