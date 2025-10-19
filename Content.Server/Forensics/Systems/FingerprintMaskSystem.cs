// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Content.Shared.Forensics;
using Content.Shared.Forensics.Components;
using Content.Shared.Inventory;

namespace Content.Server.Forensics;

public sealed class FingerprintMaskSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FingerprintMaskComponent, InventoryRelayedEvent<TryAccessFingerprintEvent>>(OnTryAccessFingerprint);
    }

    private void OnTryAccessFingerprint(EntityUid uid, FingerprintMaskComponent comp, ref InventoryRelayedEvent<TryAccessFingerprintEvent> args)
    {
        if (args.Args.Cancelled)
            return;

        args.Args.Blocker = uid;
        args.Args.Cancel();
    }
}
