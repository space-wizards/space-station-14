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
public enum MechAssemblyVisuals : byte
{
    State
}

[Serializable, NetSerializable]
public enum MechVisualLayers : byte
{
    Base
}

/// <summary>
/// Event raised on equipment when it is inserted into a mech
/// </summary>
[ByRefEvent]
public readonly struct MechEquipmentInsertedEvent
{
    public readonly EntityUid Mech;

    public MechEquipmentInsertedEvent(EntityUid mech)
    {
        Mech = mech;
    }
}

/// <summary>
/// Event raised on equipment when it is removed from a mech
/// </summary>
[ByRefEvent]
public readonly struct MechEquipmentRemovedEvent
{
    public readonly EntityUid Mech;

    public MechEquipmentRemovedEvent(EntityUid mech)
    {
        Mech = mech;
    }
}

[Serializable, NetSerializable]
public sealed class MechBoundUserInterfaceState : BoundUserInterfaceState
{

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

public sealed class MechEjectPilotEvent : InstantActionEvent
{
}
