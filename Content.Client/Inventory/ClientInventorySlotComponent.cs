using Content.Client.Items.UI;
using Content.Shared.Inventory;

namespace Content.Client.Inventory;

[RegisterComponent]
public sealed class ClientInventorySlotComponent : InventorySlotComponent
{
    public readonly Dictionary<string, ItemSlotButton> SlotButtons = new();
    public readonly Dictionary<string, int> SlotDefIndexes = new();
}
