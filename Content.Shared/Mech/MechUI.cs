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

/// <summary>
/// UI event raised to remove a piece of equipment from a mech
/// </summary>
[Serializable, NetSerializable]
public sealed class MechEquipmentRemoveMessage : BoundUserInterfaceMessage
{
    public EntityUid Equipment;

    public MechEquipmentRemoveMessage(EntityUid equipment)
    {
        Equipment = equipment;
    }
}

/// <summary>
/// base for all mech ui messages
/// </summary>
[Serializable, NetSerializable]
public abstract class MechEquipmentUiMessage : BoundUserInterfaceMessage
{
    public EntityUid Equipment;
}

/// <summary>
/// event raised for the grabber equipment to eject an item from it's storage
/// </summary>
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

/// <summary>
/// BUI state for mechs that also contains all equipment ui states.
/// </summary>
/// <remarks>
///    ⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⡠⢐⠤⢃⢰⠐⡄⣀⠀⠀
///    ⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⠔⣨⠀⢁⠁⠐⡐⠠⠜⠐⠀
///    ⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠔⠐⢀⡁⣀⠔⡌⠡⢀⢐⠁⠀
///    ⠀⠀⠀⠀⢀⠔⠀⡂⡄⠠⢀⡀⠀⣄⡀⠠⠤⠴⡋⠑⡠⠀⠔⠐⢂⠕⢀⡂⠀⠀
///    ⠀⠀⠀⡔⠁⠠⡐⠁⠀⠀⠀⢘⠀⠀⠀⠀⠠⠀⠈⠪⠀⠑⠡⣃⠈⠤⡈⠀⠀⠀
///    ⠀⠀⠨⠀⠄⡒⠀⡂⢈⠀⣀⢌⠀⠀⠁⡈⠀⢆⢀⠀⡀⠉⠒⢆⠑⠀⠀⠀⠀⠀
///    ⠀⠀⠀⡁⠐⠠⠐⡀⠀⢀⣀⠣⡀⠢⡀⠀⢀⡃⠰⠀⠈⠠⢁⠎⠀⠀⠀⠀⠀⠀
///    ⠀⠀⠀⠅⠒⣈⢣⠠⠈⠕⠁⠱⠄⢤⠈⠪⠡⠎⢘⠈⡁⢙⠈⠀⠀⠀⠀⠀⠀⠀
///    ⠀⠀⠀⠃⠀⢡⠀⠧⠀⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀⢕⡈⠌⠀⠀⠀⠀⠀⠀⠀⠀
///    ⠀⠀⠀⠀⠀⠀⠈⡀⡀⡆⠀⠀⠀⠀⠀⠀⠀⠀⠀⡰⠀⡐⠀⠀⠀⠀⠀⠀⠀⠀
///    ⠀⠀⠀⠀⠀⠀⠀⢈⢂⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠸⠀⡃⠀⠀⠀⠀⠀⠀⠀⠀
///    ⠀⠀⠀⠀⠀⠀⠀⠎⠐⢅⠀⠀⠀⠀⠀⠀⠀⠀⠀⢐⠅⠚⠄⠀⠀⠀⠀⠀⠀⠀
///    ⠀⠀⢈⠩⠈⠀⠐⠁⠀⢀⠀⠄⡂⠒⠐⠀⠆⠁⠰⠠⠀⢅⠈⠐⠄⢁⢡⠀⠀⠀
///    ⠀⠀⢈⡀⠰⡁⠀⠁⠴⠁⠔⠀⠀⠄⠄⡁⠀⠂⠀⠢⠠⠁⠀⠠⠈⠂⠬⠀⠀⠀
///    ⠀⠀⠠⡂⢄⠤⠒⣁⠐⢕⢀⡈⡐⡠⠄⢐⠀⠈⠠⠈⡀⠂⢀⣀⠰⠁⠠⠀⠀
/// trojan horse bui state⠀
/// </remarks>
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
