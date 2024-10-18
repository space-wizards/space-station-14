using Content.Shared.BroadcastInteractionUsingToContainer.Components;
using Content.Shared.BroadcastInteractionUsingToContainer.Systems;
using Content.Shared.Interaction;
using Robust.Shared.Map;

namespace Content.Shared.BroadcastInteractionUsingToContainer;

/// <summary>
/// Raised if entity with <see cref="BroadcastInteractUsingToContainerComponent"/> interact with something.
/// Raised after <see cref="BeforeRangedInteractEvent"/> and before <see cref="InteractUsingEvent"/>.
/// </summary>
public sealed class InteractBeforeUsingWithInContainerEvent(EntityUid user, EntityUid used,
                                    EntityUid? target, EntityCoordinates clickLocation, bool canReach)
                                    : InteractEvent(user, used, target, clickLocation, canReach)
{ }

/// <summary>
/// Raised if entity with <see cref="BroadcastInteractUsingTargetToContainerComponent"/> interact with something.
/// Raised after <see cref="InteractUsingEvent"/> and before <see cref="AfterInteractEvent"/>.
/// </summary>
public sealed class InteractUsingTargetInContainerEvent(EntityUid user, EntityUid used,
                                    EntityUid? target, EntityCoordinates clickLocation, bool canReach)
                                    : InteractEvent(user, used, target, clickLocation, canReach)
{ }

/// <summary>
/// Raised if entity with <see cref="BroadcastAfterInteractUsingToContainerComponent"/> interact with something.
/// Raised after <see cref="AfterInteractEvent"/>.
/// </summary>
public sealed class InteractAfterUsingWithInContainerEvent(EntityUid user, EntityUid used, EntityUid? target,
                                                        EntityCoordinates clickLocation, bool canReach)
                                                        : InteractEvent(user, used, target, clickLocation, canReach)
{ }

/// <summary>
/// Used to wrap events for generic methods in <see cref="BroadcastInteractUsingToContainerSystem"/>
/// </summary>
public struct EventWrapper
{
    public EntityUid User => _interactEvent.User;
    public EntityUid Used => _interactEvent.Used;
    public EntityUid? Target => _interactEvent.Target;
    public EntityCoordinates ClickLocation => _interactEvent.ClickLocation;
    public bool CanReach => _interactEvent.CanReach;
    public bool Handled
    {
        get => _interactEvent.Handled;
        set
        {
            _interactEvent.Handled = value;
        }
    }

    private InteractEvent _interactEvent;

    public EventWrapper(EntityUid user, EntityUid used, EntityUid? target,
                        EntityCoordinates clickLocation, bool canReach, bool handled)
    {
        //just a dummy event type
        _interactEvent = new AfterInteractUsingEvent(user, used, target, clickLocation, canReach);
        {
            Handled = handled;
        };
    }

    public EventWrapper(InteractEvent interactEvent)
    {
        _interactEvent = interactEvent;
    }

    public EventWrapper(InteractUsingEvent beforeInteract)
        : this(beforeInteract.User, beforeInteract.Used, beforeInteract.Target,
        beforeInteract.ClickLocation, true, beforeInteract.Handled)
    { }

    public EventWrapper(BeforeRangedInteractEvent beforeRangedInteract)
        : this(beforeRangedInteract.User, beforeRangedInteract.Used, beforeRangedInteract.Target,
        beforeRangedInteract.ClickLocation, beforeRangedInteract.CanReach, beforeRangedInteract.Handled)
    { }
}
