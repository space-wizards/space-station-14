using Content.Client.Hands.UI;
using Content.Client.Items;
using Content.Shared.Inventory.VirtualItem;

namespace Content.Client.Inventory;

public sealed class VirtualItemSystem : SharedVirtualItemSystem
{
    public override void Initialize()
    {
        base.Initialize();

        Subs.ItemStatus<VirtualItemComponent>(_ => new HandVirtualItemStatus());
    }
}
