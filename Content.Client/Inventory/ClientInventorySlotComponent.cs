using Content.Client.Items.UI;
using Content.Shared.Inventory;

namespace Content.Client.Inventory;

[RegisterComponent]
[ComponentReference(typeof(InventorySlotComponent))]
public sealed class ClientInventorySlotComponent : InventorySlotComponent
{
    public readonly Dictionary<string, (ItemSlotButton hudButton, ItemSlotButton windowButton)> SlotButtons = new();
    public readonly Dictionary<string, int> SlotDefIndexes = new();
}
