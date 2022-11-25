using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared.Mech;

[Serializable, NetSerializable]
public enum MechUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public enum MechVisuals : byte
{
    Open, //whether or not it's open and has a rider
    Broken //if it broke and no longer works.
}

[Serializable, NetSerializable]
public enum MechVisualLayers : byte
{
    Base
}

/// <summary>
/// Used to send information about mech equipment to the
/// client for UI display.
/// </summary>
[Serializable, NetSerializable]
public sealed class MechEquipmentUiInformation
{
    public EntityUid Equipment;

    public bool CanBeEnabled = false;

    public bool CanBeRemoved = true;

    public MechEquipmentUiInformation(EntityUid equipment)
    {
        Equipment = equipment;
    }
}

[ByRefEvent]
public struct EquipmentGetInformationEvent
{
    public MechEquipmentUiInformation Information;

    public EquipmentGetInformationEvent(MechEquipmentUiInformation information)
    {
        Information = information;
    }
}

/// <summary>
/// Toggles a piece of mech equipment either on or off
/// </summary>
[ByRefEvent]
public struct MechEquipmentToggleEvent
{
    public bool Enabled;

    public MechEquipmentToggleEvent(bool enabled)
    {
        Enabled = enabled;
    }
}

[Serializable, NetSerializable]
public sealed class MechBoundUserInterfaceState : BoundUserInterfaceState
{
    public List<MechEquipmentUiInformation> EquipmentInfo = new();
}

[Serializable, NetSerializable]
public sealed class MechEquipmentToggleMessage : BoundUserInterfaceMessage
{
    public EntityUid Equipment;

    public bool Enabled;

    public MechEquipmentToggleMessage(EntityUid equipment, bool enabled)
    {
        Equipment = equipment;
        Enabled = enabled;
    }
}

[Serializable, NetSerializable]
public sealed class MechEquipmentRemoveMessage : BoundUserInterfaceMessage
{
    public EntityUid Equipment;

    public MechEquipmentRemoveMessage(EntityUid equipment)
    {
        Equipment = equipment;
    }
}

public sealed class MechToggleEquipmentEvent : InstantActionEvent
{

}

public sealed class MechOpenUiEvent : InstantActionEvent
{

}
