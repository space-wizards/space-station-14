using Content.Shared.Actions.ActionTypes;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Actions;

public sealed class GetActionsEvent : EntityEventArgs
{
    public SortedSet<ActionType> Actions = new();
}

/// <summary>
///     Event used to communicate with the client that the user wishes to perform some action.
/// </summary>
/// <remarks>
///     Basically a wrapper for <see cref="PerformActionEvent"/> that the action system will validate before performing
///     (check cooldown, target, enabling-entity)
/// </remarks>
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

[ImplicitDataDefinitionForInheritors]
public abstract class PerformActionEvent : HandledEntityEventArgs
{
    /// <summary>
    ///     The user performing the action
    /// </summary>
    public EntityUid Performer;
}

public abstract class PerformEntityTargetActionEvent : PerformActionEvent
{
    public EntityUid Target;
}

public abstract class PerformWorldTargetActionEvent : PerformActionEvent
{
    public MapCoordinates Target;
}
