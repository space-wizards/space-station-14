using Robust.Shared.Serialization;

namespace Content.Shared.Mech;

[Serializable, NetSerializable]
public enum MechUiKey : byte
{
    Key
}

/// <summary>
/// Event raised to collect BUI states for each of the mech's equipment items
/// </summary>
public sealed class MechEquipmentUiStateReadyEvent : EntityEventArgs
{
    public Dictionary<EntityUid, BoundUserInterfaceState> States = new();
}

/// <summary>
/// Event raised to relay an equipment ui message
/// </summary>
public sealed class MechEquipmentUiMessageRelayEvent : EntityEventArgs
{
    public MechEquipmentUiMessage Message;

    public MechEquipmentUiMessageRelayEvent(MechEquipmentUiMessage message)
    {
        Message = message;
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

[Serializable, NetSerializable]
public abstract class MechEquipmentUiMessage : BoundUserInterfaceMessage
{
    public EntityUid Equipment;
}

[Serializable, NetSerializable]
public sealed class MechGrabberEjectMessage : MechEquipmentUiMessage
{
    public EntityUid Item;

    public MechGrabberEjectMessage(EntityUid equipment, EntityUid uid)
    {
        Equipment = equipment;
        Item = uid;
    }
}

[Serializable, NetSerializable]
public sealed class MechBoundUiState : BoundUserInterfaceState
{
    public Dictionary<EntityUid, BoundUserInterfaceState> EquipmentStates = new();
}

[Serializable, NetSerializable]
public sealed class MechGrabberUiState : BoundUserInterfaceState
{
    public List<EntityUid> Contents = new();
    public int MaxContents;
}
