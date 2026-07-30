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
