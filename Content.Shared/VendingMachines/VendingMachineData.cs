using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared.VendingMachines;

[Serializable, NetSerializable]
public sealed class VendingMachineInventoryEntry
{
    [ViewVariables(VVAccess.ReadWrite)]
    public string ItemId;

    [ViewVariables(VVAccess.ReadWrite)]
    public string TypeId;

    [ViewVariables(VVAccess.ReadWrite)]
    public uint Amount;

    public VendingMachineInventoryEntry(string itemId, string typeId, uint amount)
    {
        ItemId = itemId;
        TypeId = typeId;
        Amount = amount;
    }
}

[Serializable, NetSerializable]
public enum VendingMachineVisuals
{
    VisualState,
}

[Serializable, NetSerializable]
public enum VendingMachineVisualState
{
    Normal,
    Off,
    Broken,
    Eject,
    Deny,
}

public enum VendingMachineVisualLayers : byte
{
    /// <summary>
    /// Off / Broken. The other layers will overlay this if the machine is on.
    /// </summary>
    Base,
    /// <summary>
    /// Normal / Deny / Eject
    /// </summary>
    BaseUnshaded,
    /// <summary>
    /// Screens that are persistent (where the machine is not off or broken)
    /// </summary>
    Screen,
}

[Serializable, NetSerializable]
public enum ContrabandWireKey : byte
{
    StatusKey,
    TimeoutKey,
}

[Serializable, NetSerializable]
public enum EjectWireKey : byte
{
    StatusKey,
}

public sealed partial class VendingMachineSelfDispenseEvent : InstantActionEvent
{

};
