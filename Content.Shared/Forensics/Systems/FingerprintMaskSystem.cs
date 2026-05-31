using Content.Shared.Forensics.Components;
using Content.Shared.Forensics.Events;
using Content.Shared.Inventory;

namespace Content.Shared.Forensics.Systems;

public sealed class FingerprintMaskSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FingerprintMaskComponent, InventoryRelayedEvent<TryAccessFingerprintEvent>>(OnTryAccessFingerprint);
    }

    private void OnTryAccessFingerprint(Entity<FingerprintMaskComponent> gloves, ref InventoryRelayedEvent<TryAccessFingerprintEvent> args)
    {
        if (args.Args.Blocker.HasValue)
            return;

        args.Args.Blocker = gloves.Owner;
    }
}
