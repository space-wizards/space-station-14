namespace Content.Shared.Inventory;

[Virtual]
[Friend(typeof(InventorySystem))]
[ComponentProtoName("InventorySlot")]
public class InventorySlotComponent : Component
{
    [DataField("slots")]
    public SlotDefinition[] Slots { get; } = Array.Empty<SlotDefinition>();
}
