using Content.Shared.Hands.Components;
using Content.Shared.Inventory.Events;
using Robust.Shared.GameObjects;

namespace Content.Shared.Hands;

public abstract class SharedHandVirtualItemSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HandVirtualItemComponent, BeingEquippedAttemptEvent>(OnBeingEquippedAttempt);
    }

    private void OnBeingEquippedAttempt(EntityUid uid, HandVirtualItemComponent component, BeingEquippedAttemptEvent args)
    {
        args.Cancel();
    }
}
