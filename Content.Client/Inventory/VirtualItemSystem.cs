using Content.Client.Hands.UI;
using Content.Client.Items;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Item;
using Robust.Shared.Prototypes;

namespace Content.Client.Inventory;

public sealed partial class VirtualItemSystem : SharedVirtualItemSystem
{
    public static readonly ProtoId<ItemStatusPrototype> VirtualItemItemStatus = "VirtualItem";

    public override void Initialize()
    {
        base.Initialize();

        Subs.ItemStatus<VirtualItemComponent>(_ => new HandVirtualItemStatus(), VirtualItemItemStatus);
    }
}
