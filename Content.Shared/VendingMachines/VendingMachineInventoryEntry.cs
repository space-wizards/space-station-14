using Robust.Shared.Serialization;

namespace Content.Shared.VendingMachines;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class VendingMachineInventoryEntry
{
    [DataField]
    public InventoryType Type;

    [DataField]
    public string ID;

    [DataField]
    public uint Amount;

    public VendingMachineInventoryEntry(InventoryType type, string id, uint amount)
    {
        Type = type;
        ID = id;
        Amount = amount;
    }

    public VendingMachineInventoryEntry(VendingMachineInventoryEntry entry)
    {
        Type = entry.Type;
        ID = entry.ID;
        Amount = entry.Amount;
    }
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

    public TimeSpan? EjectEnd;

    public TimeSpan? DenyEnd;

    public TimeSpan? DispenseOnHitEnd;

    public bool Broken;
}
