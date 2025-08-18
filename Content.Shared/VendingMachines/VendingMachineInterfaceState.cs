using Robust.Shared.Serialization;

namespace Content.Shared.VendingMachines;

[Serializable, NetSerializable]
public sealed class VendingMachineEjectMessage(InventoryType type, string id) : BoundUserInterfaceMessage
{
    public readonly InventoryType Type = type;
    public readonly string ID = id;
}

[Serializable, NetSerializable]
public enum VendingMachineUiKey
{
    Key,
}
