using System.Linq;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;

namespace Content.Shared.Item;

public abstract class SharedMultiHandedItemSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MultiHandedItemComponent, GettingPickedUpAttemptEvent>((e,c,a) => OnAttemptPickup(e,c,a));
        SubscribeLocalEvent<MultiHandedItemComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);
        SubscribeLocalEvent<MultiHandedItemComponent, GotEquippedHandEvent>(OnEquipped);
        SubscribeLocalEvent<MultiHandedItemComponent, GotUnequippedHandEvent>(OnUnequipped);
    }

    protected virtual void OnEquipped(EntityUid uid, MultiHandedItemComponent component, GotEquippedHandEvent args)
    {

    }

    protected virtual void OnUnequipped(EntityUid uid, MultiHandedItemComponent component, GotUnequippedHandEvent args)
    {

    }

    protected virtual bool OnAttemptPickup(EntityUid uid, MultiHandedItemComponent component, GettingPickedUpAttemptEvent args)
    {
        if (!TryComp<SharedHandsComponent>(args.User, out var hands))
        {
            args.Cancel();
            return false;
        }

        var freeHands = _hands.EnumerateHands(args.User, hands).Where(x => x.IsEmpty);
        if (freeHands.Count() < component.HandsNeeded)
        {
            args.Cancel();
            return false;
        }

        return true;
    }

    private void OnVirtualItemDeleted(EntityUid uid, MultiHandedItemComponent component, VirtualItemDeletedEvent args)
    {
        if (args.BlockingEntity != uid)
            return;

        _hands.TryDrop(args.User, uid);
    }
}
