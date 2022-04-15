using Content.Shared.Inventory;

namespace Content.Server.Inventory;

[RegisterComponent]
[ComponentReference(typeof(InventorySlotComponent))]
public sealed class ServerInventorySlotComponent : InventorySlotComponent { }
