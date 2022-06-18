using Content.Shared.Actions.ActionTypes;

namespace Content.Shared.Zombies;
/// <summary>
///     Event raised whenever an entity is zombified.
///     Used by the zombie gamemode to track total infections.
/// </summary>
public sealed class EntityZombifiedEvent : EventArgs
{
    /// <summary>
    ///     The entity that was zombified.
    /// </summary>
    public EntityUid Target;

    public EntityZombifiedEvent(EntityUid target)
    {
        Target = target;
    }
};

/// <summary>
///     Event raised when a player zombifies themself using the "turn" action
/// </summary>
public sealed class ZombifySelfActionEvent : InstantAction { };
