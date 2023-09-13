using Content.Shared.Hands;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Shared.Map;
using Robust.Shared.Network;
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
    private readonly IEntityManager _entities;
    private readonly INetManager _net;
    public readonly SortedSet<EntityUid> Actions = new();

    /// <summary>
    /// User equipping the item.
    /// </summary>
    public EntityUid User;

    /// <summary>
    ///     Slot flags for the inventory slot that this item got equipped to. Null if not in a slot (i.e., if equipped to hands).
    /// </summary>
    public SlotFlags? SlotFlags;

    /// <summary>
    ///     If true, the item was equipped to a users hands.
    /// </summary>
    public bool InHands => SlotFlags == null;

    public GetItemActionsEvent(IEntityManager entities, INetManager net, EntityUid user, SlotFlags? slotFlags = null)
    {
        _entities = entities;
        _net = net;
        User = user;
        SlotFlags = slotFlags;
    }

    public void AddAction(ref EntityUid? actionId, string? prototypeId)
    {
        if (_entities.Deleted(actionId))
        {
            if (string.IsNullOrWhiteSpace(prototypeId) || _net.IsClient)
                return;

            actionId = _entities.Spawn(prototypeId);
        }

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
}
