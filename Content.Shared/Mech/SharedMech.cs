using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared.Mech;

[Serializable, NetSerializable]
public enum MechVisuals : byte
{
    Open, //whether or not it's open and has a rider
    Broken //if it's in broken state
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
/// Raised on equipment when it is inserted into a mech
/// </summary>
[ByRefEvent]
public readonly record struct MechEquipmentInsertedEvent(EntityUid Mech)
{
    public readonly EntityUid Mech = Mech;
}

/// <summary>
/// Raised on equipment when it is removed from a mech
/// </summary>
[ByRefEvent]
public readonly record struct MechEquipmentRemovedEvent(EntityUid Mech)
{
    public readonly EntityUid Mech = Mech;
}

/// <summary>
/// Raised on the mech equipment before it is going to be removed.
/// </summary>
[ByRefEvent]
public record struct AttemptRemoveMechEquipmentEvent()
{
    public bool Cancelled = false;
}

/// <summary>
/// Raised on module when it is inserted into a mech
/// </summary>
[ByRefEvent]
public readonly record struct MechModuleInsertedEvent(EntityUid Mech)
{
    public readonly EntityUid Mech = Mech;
}

public sealed partial class MechToggleEquipmentEvent : InstantActionEvent
{
}

public sealed class MechOpenEquipmentRadialEvent : EntityEventArgs
{
}

public sealed partial class MechOpenUiEvent : InstantActionEvent
{
}

public sealed partial class MechEjectPilotEvent : InstantActionEvent
{
}

[Serializable, NetSerializable]
public sealed partial class RequestMechEquipmentSelectEvent : EntityEventArgs
{
    public NetEntity? Equipment;
}
