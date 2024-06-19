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
    private readonly ActionContainerSystem _system;
    public readonly SortedSet<EntityUid> Actions = new();

    /// <summary>
    /// User equipping the item.
    /// </summary>
    public EntityUid User;

    /// <summary>
    /// The entity that is being asked to provide the actions. This is used as a default argument to <see cref="AddAction(ref System.Nullable{Robust.Shared.GameObjects.EntityUid},string,Robust.Shared.GameObjects.EntityUid)"/>.
    /// I.e., if a new action needs to be spawned, then it will be inserted into this entity unless otherwise specified.
    /// </summary>
    public EntityUid Provider;

    /// <summary>
    ///     Slot flags for the inventory slot that this item got equipped to. Null if not in a slot (i.e., if equipped to hands).
    /// </summary>
    public SlotFlags? SlotFlags;

    /// <summary>
    ///     If true, the item was equipped to a users hands.
    /// </summary>
    public bool InHands => SlotFlags == null;

    public GetItemActionsEvent(ActionContainerSystem system, EntityUid user, EntityUid provider, SlotFlags? slotFlags = null)
    {
        _system = system;
        User = user;
        Provider = provider;
        SlotFlags = slotFlags;
    }

    /// <summary>
    /// Grant the given action. If the EntityUid does not refer to a valid action entity, it will create a new action and
    /// store it in <see cref="container"/>.
    /// </summary>
    public void AddAction(ref EntityUid? actionId, string prototypeId, EntityUid container)
    {
        if (_system.EnsureAction(container, ref actionId, prototypeId))
            Actions.Add(actionId.Value);
    }

    /// <summary>
    /// Grant the given action. If the EntityUid does not refer to a valid action entity, it will create a new action and
    /// store it in <see cref="Provider"/>.
    /// </summary>
    public void AddAction(ref EntityUid? actionId, string prototypeId)
    {
        AddAction(ref actionId, prototypeId, Provider);
    }

    public void AddAction(EntityUid? actionId)
    {
        if (actionId != null)
            Actions.Add(actionId.Value);
    }
}

/// <summary>
///     Event used to communicate with the server that a client wishes to perform some action.
/// </summary>
[Serializable, NetSerializable]
public sealed class RequestPerformActionEvent : EntityEventArgs
{
    public readonly NetEntity Action;
    public readonly NetEntity? EntityTarget;
    public readonly NetCoordinates? EntityCoordinatesTarget;

    public RequestPerformActionEvent(NetEntity action)
    {
        Action = action;
    }

    public RequestPerformActionEvent(NetEntity action, NetEntity entityTarget)
    {
        Action = action;
        EntityTarget = entityTarget;
    }

    public RequestPerformActionEvent(NetEntity action, NetCoordinates entityCoordinatesTarget)
    {
        Action = action;
        EntityCoordinatesTarget = entityCoordinatesTarget;
    }
}

/// <summary>
///     This is the type of event that gets raised when an <see cref="InstantAction"/> is performed. The <see
///     cref="Performer"/> field is automatically filled out by the <see cref="SharedActionsSystem"/>.
/// </summary>
/// <remarks>
///     To define a new action for some system, you need to create an event that inherits from this class.
/// </remarks>
public abstract partial class InstantActionEvent : BaseActionEvent { }

/// <summary>
///     This is the type of event that gets raised when an <see cref="EntityTargetAction"/> is performed. The <see
///     cref="Performer"/> and <see cref="Target"/> fields will automatically be filled out by the <see
///     cref="SharedActionsSystem"/>.
/// </summary>
/// <remarks>
///     To define a new action for some system, you need to create an event that inherits from this class.
/// </remarks>
public abstract partial class EntityTargetActionEvent : BaseActionEvent
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
public abstract partial class WorldTargetActionEvent : BaseActionEvent
{
    /// <summary>
    ///     The coordinates of the location that the user targeted.
    /// </summary>
    public EntityCoordinates Target;
}

/// <summary>
///     Base class for events that are raised when an action gets performed. This should not generally be used outside of the action
///     system.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class BaseActionEvent : HandledEntityEventArgs
{
    /// <summary>
    ///     The user performing the action.
    /// </summary>
    public EntityUid Performer;

    /// <summary>
    ///     The action the event belongs to.
    /// </summary>
    public EntityUid Action;
}
