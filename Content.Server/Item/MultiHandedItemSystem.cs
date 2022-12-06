using Content.Server.Hands.Systems;
using Content.Shared.Hands;
using Content.Shared.Item;

namespace Content.Server.Item;

public sealed class MultiHandedItemSystem : SharedMultiHandedItemSystem
{
    [Dependency] private readonly HandVirtualItemSystem _virtualItem = default!;

    protected override void OnEquipped(EntityUid uid, MultiHandedItemComponent component, GotEquippedHandEvent args)
    {
        for (var i = 0; i < component.HandsNeeded - 1; i++)
        {
            _virtualItem.TrySpawnVirtualItemInHand(uid, args.User);
        }
    }

    protected override void OnUnequipped(EntityUid uid, MultiHandedItemComponent component, GotUnequippedHandEvent args)
    {
        _virtualItem.DeleteInHandsMatching(args.User, uid);
    }
}
