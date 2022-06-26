using Content.Shared.Actions.ActionTypes;
using Content.Shared.Hands;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Actions;

/// <summary>
///     Event raised directed at items or clothing when they are equipped or held. In order for an item to grant actions some
///     system can subscribe to this event and add actions to the <see cref="Actions"/> list.
/// </summary>
/// <remarks>
///     Note that a system could also just manually add actions as a result of a <see cref="GotEquippedEvent"/> or <see
///     cref="GotEquippedHandEvent"/>. This exists mostly as a convenience event, while also helping to keep
///     action-granting logic separate from general equipment behavior.
/// </remarks>
public sealed class GetItemActionsEvent : EntityEventArgs
{
    public SortedSet<ActionType> Actions = new();

    /// <summary>
    ///     Slot flags for the inventory slot that this item got equipped to. Null if not in a slot (i.e., if equipped to hands).
    /// </summary>
    public SlotFlags? SlotFlags;

    /// <summary>
    ///     If true, the item was equipped to a users hands.
    /// </summary>
    public bool InHands => SlotFlags == null;

    public GetItemActionsEvent(SlotFlags? slotFlags = null)
    {
        SlotFlags = slotFlags;
    }
}

/// <summary>
///     Event used to communicate with the server that a client wishes to perform some action.
/// </summary>
[Serializable, NetSerializable]
public sealed class RequestPerformActionEvent : EntityEventArgs
{
    public readonly ActionType Action;
    public readonly EntityUid? EntityTarget;
    public readonly MapCoordinates? MapTarget;

    public RequestPerformActionEvent(InstantAction action)
    {
        Action = action;
    }

    public RequestPerformActionEvent(EntityTargetAction action, EntityUid entityTarget)
    {
        Action = action;
        EntityTarget = entityTarget;
    }

    public RequestPerformActionEvent(WorldTargetAction action, MapCoordinates mapTarget)
    {
        Action = action;
        MapTarget = mapTarget;
    }
}

/// <summary>
///     This is the type of event that gets raised when an <see cref="InstantAction"/> is performed. The <see
///     cref="Performer"/> field is automatically filled out by the <see cref="SharedActionsSystem"/>.
/// </summary>
/// <remarks>
///     To define a new action for some system, you need to create an event that inherits from this class.
/// </remarks>
public abstract class InstantActionEvent : BaseActionEvent { }

/// <summary>
///     This is the type of event that gets raised when an <see cref="EntityTargetAction"/> is performed. The <see
///     cref="Performer"/> and <see cref="Target"/> fields will automatically be filled out by the <see
///     cref="SharedActionsSystem"/>.
/// </summary>
/// <remarks>
///     To define a new action for some system, you need to create an event that inherits from this class.
/// </remarks>
public abstract class EntityTargetActionEvent : BaseActionEvent
{
    /// <summary>
    ///     The entity that the user targeted.
    /// </summary>
    public EntityUid Target;
}

/// <summary>
///     This is the type of event that gets raised when an <see cref="WorldTargetAction"/> is performed. The <see
///     cref="Performer"/> and <see cref="Target"/> fields will automatically be filled out by the <see
///     cref="SharedActionsSystem"/>.
/// </summary>
/// <remarks>
///     To define a new action for some system, you need to create an event that inherits from this class.
/// </remarks>
public abstract class WorldTargetActionEvent : BaseActionEvent
{
    /// <summary>
    ///     The coordinates of the location that the user targeted.
    /// </summary>
    public MapCoordinates Target;
}

/// <summary>
///     Base class for events that are raised when an action gets performed. This should not generally be used outside of the action
///     system.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract class BaseActionEvent : HandledEntityEventArgs
{
    /// <summary>
    ///     The user performing the action.
    /// </summary>
    public EntityUid Performer;
}
