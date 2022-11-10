using Content.Server.Hands.Systems;
using Content.Shared.Hands;
using Content.Shared.Item;
using Content.Shared.Popups;
using Robust.Shared.Player;

namespace Content.Server.Item;

public sealed class MultiHandedItemSystem : SharedMultiHandedItemSystem
{
    [Dependency] private readonly HandVirtualItemSystem _virtualItem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    protected override void OnEquipped(EntityUid uid, MultiHandedItemComponent component, GotEquippedHandEvent args)
    {
        base.OnEquipped(uid, component, args);

        for (var i = 0; i < component.HandsNeeded - 1; i++)
        {
            _virtualItem.TrySpawnVirtualItemInHand(uid, args.User);
        }
    }

    protected override void OnUnequipped(EntityUid uid, MultiHandedItemComponent component, GotUnequippedHandEvent args)
    {
        base.OnUnequipped(uid, component, args);

        _virtualItem.DeleteInHandsMatching(args.User, uid);
    }

    protected override bool OnAttemptPickup(EntityUid uid, MultiHandedItemComponent component, GettingPickedUpAttemptEvent args)
    {
        if (!base.OnAttemptPickup(uid, component, args))
        {
            _popup.PopupEntity(Loc.GetString("multi-handed-item-pick-up-fail",
                ("number", component.HandsNeeded - 1), ("item", uid)), args.User, Filter.Entities(args.User));
            return false;
        }
        return true;
    }
}
