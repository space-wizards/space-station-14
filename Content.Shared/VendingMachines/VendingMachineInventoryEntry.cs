using Robust.Shared.Serialization;

namespace Content.Shared.VendingMachines;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class VendingMachineInventoryEntry(InventoryType type, string id, uint amount)
{
    [DataField]
    public InventoryType Type = type;

    [DataField]
    public string ID = id;

    [DataField]
    public uint Amount = amount;

    public VendingMachineInventoryEntry(VendingMachineInventoryEntry entry) : this(entry.Type, entry.ID, entry.Amount) { }
}

[Serializable, NetSerializable]
public enum InventoryType : byte
{
    Regular,
    Emagged,
    Contraband
}

[Serializable, NetSerializable]
public sealed class VendingMachineComponentState : ComponentState
{
    public Dictionary<string, VendingMachineInventoryEntry> Inventory = new();

    public Dictionary<string, VendingMachineInventoryEntry> EmaggedInventory = new();

    public Dictionary<string, VendingMachineInventoryEntry> ContrabandInventory = new();

    public bool Contraband;

    public TimeSpan? DispenseOnHitEnd;

    public bool Broken;
}
