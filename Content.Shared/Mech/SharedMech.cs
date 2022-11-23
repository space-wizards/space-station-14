using Robust.Shared.Serialization;

namespace Content.Shared.Mech;

[Serializable, NetSerializable]
public enum MechUiKey : byte
{
    Key
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
    public List<MechEquipmentUiInformation> EquipmentInfo = new List<MechEquipmentUiInformation>();
}
