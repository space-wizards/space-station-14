using Content.Server.Wires;
using Content.Shared.Radio.Components;

namespace Content.Server.Radio.EntitySystems;

public sealed class TelecomSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TelecomServerComponent, EncryptionKeyInsertAttempt>(OnKeyInsertAttempt);
        SubscribeLocalEvent<TelecomServerComponent, EncryptionKeyRemovalAttempt>(OnKeyRemovalAttempt);
    }

    private void OnKeyInsertAttempt(EntityUid uid, TelecomServerComponent component, ref EncryptionKeyInsertAttempt args)
    {
        if (TryComp<WiresComponent>(uid, out var wires) && !wires.IsPanelOpen)
            args.Cancelled = true;
    }

    private void OnKeyRemovalAttempt(EntityUid uid, TelecomServerComponent component, ref EncryptionKeyRemovalAttempt args)
    {
        if (TryComp<WiresComponent>(uid, out var wires) && !wires.IsPanelOpen)
            args.Cancelled = true;
    }
}
