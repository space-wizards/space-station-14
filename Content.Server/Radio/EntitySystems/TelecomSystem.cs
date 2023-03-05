using Content.Server.Wires;
using Content.Shared.Radio.Components;

namespace Content.Server.Radio.EntitySystems;

public sealed class TelecomSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TelecomServerComponent, EncryptionKeyInsertEvent>(OnKeyInsert);
        SubscribeLocalEvent<TelecomServerComponent, EncryptionKeyRemovalEvent>(OnKeyRemoval);
    }

    private void OnKeyInsert(EntityUid uid, TelecomServerComponent component, ref EncryptionKeyInsertEvent args)
    {
        if (TryComp<WiresComponent>(uid, out var wires))
        {
            args.KeyHolder.KeysUnlocked = wires.IsPanelOpen;
        }
    }

    private void OnKeyRemoval(EntityUid uid, TelecomServerComponent component, ref EncryptionKeyRemovalEvent args)
    {
        if (TryComp<WiresComponent>(uid, out var wires))
        {
            args.KeyHolder.KeysUnlocked = wires.IsPanelOpen;
        }
    }
}
