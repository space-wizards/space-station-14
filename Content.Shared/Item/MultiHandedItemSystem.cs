using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Shared.Item;

public sealed class MultiHandedItemSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedVirtualItemSystem _virtualItem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MultiHandedItemComponent, GettingPickedUpAttemptEvent>(OnAttemptPickup);
        SubscribeLocalEvent<MultiHandedItemComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);
        SubscribeLocalEvent<MultiHandedItemComponent, GotEquippedHandEvent>(OnEquipped);
        SubscribeLocalEvent<MultiHandedItemComponent, GotUnequippedHandEvent>(OnUnequipped);
    }

    private void OnEquipped(EntityUid uid, MultiHandedItemComponent component, GotEquippedHandEvent args)
    {
        for (var i = 0; i < component.HandsNeeded - 1; i++)
        {
            _virtualItem.TrySpawnVirtualItemInHand(uid, args.User);
        }
    }

    private void OnUnequipped(EntityUid uid, MultiHandedItemComponent component, GotUnequippedHandEvent args)
    {
        _virtualItem.DeleteInHandsMatching(args.User, uid);
    }

    private void OnAttemptPickup(EntityUid uid, MultiHandedItemComponent component, GettingPickedUpAttemptEvent args)
    {
        if (TryComp<HandsComponent>(args.User, out var hands) && hands.CountFreeHands() >= component.HandsNeeded)
            return;

        args.Cancel();
        _popup.PopupPredictedCursor(Loc.GetString("multi-handed-item-pick-up-fail",
            ("number", component.HandsNeeded - 1), ("item", uid)), args.User);
    }

    private void OnVirtualItemDeleted(EntityUid uid, MultiHandedItemComponent component, VirtualItemDeletedEvent args)
    {
        if (args.BlockingEntity != uid || _timing.ApplyingState)
            return;

        _hands.TryDrop(args.User, uid);
    }
}
