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

    public bool CanBeEjected = true;

    public MechEquipmentUiInformation(EntityUid equipment)
    {
        Equipment = equipment;
    }
}

[ByRefEvent]
public struct MechEquipmentGetUiInformationEvent
{
    public MechEquipmentUiInformation Information;

    public MechEquipmentGetUiInformationEvent(MechEquipmentUiInformation information)
    {
        Information = information;
    }
}

[Serializable, NetSerializable]
public sealed class MechBoundUserInterfaceState : BoundUserInterfaceState
{
    public List<MechEquipmentUiInformation> EquipmentInfo = new();
}

public sealed class MechToggleEquipmentEvent : InstantActionEvent
{

}

public sealed class MechOpenUiEvent : InstantActionEvent
{

}
