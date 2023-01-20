using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared.Item;

public abstract class SharedMultiHandedItemSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MultiHandedItemComponent, GettingPickedUpAttemptEvent>(OnAttemptPickup);
        SubscribeLocalEvent<MultiHandedItemComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);
        SubscribeLocalEvent<MultiHandedItemComponent, GotEquippedHandEvent>(OnEquipped);
        SubscribeLocalEvent<MultiHandedItemComponent, GotUnequippedHandEvent>(OnUnequipped);
    }

    protected abstract void OnEquipped(EntityUid uid, MultiHandedItemComponent component, GotEquippedHandEvent args);
    protected abstract void OnUnequipped(EntityUid uid, MultiHandedItemComponent component, GotUnequippedHandEvent args);

    private void OnAttemptPickup(EntityUid uid, MultiHandedItemComponent component, GettingPickedUpAttemptEvent args)
    {
        if (TryComp<SharedHandsComponent>(args.User, out var hands) && hands.CountFreeHands() >= component.HandsNeeded)
            return;

        args.Cancel();
        if (_timing.IsFirstTimePredicted)
        {
            _popup.PopupCursor(Loc.GetString("multi-handed-item-pick-up-fail",
                ("number", component.HandsNeeded - 1), ("item", uid)), args.User);
        }
    }

    private void OnVirtualItemDeleted(EntityUid uid, MultiHandedItemComponent component, VirtualItemDeletedEvent args)
    {
        if (args.BlockingEntity != uid)
            return;

        _hands.TryDrop(args.User, uid);
    }
}
