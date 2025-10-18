using Content.Shared.Actions;

namespace Content.Shared.Zombies;

/// <summary>
///     Event that is broadcast whenever an entity is zombified.
///     Used by the zombie gamemode to track total infections.
/// </summary>
[ByRefEvent]
public readonly struct EntityZombifiedEvent
{
    /// <summary>
    ///     The entity that was zombified.
    /// </summary>
    public readonly EntityUid Target;

    public EntityZombifiedEvent(EntityUid target)
    {
        Target = target;
    }
};

/// <summary>
///     Event raised when a player zombifies themself using the "turn" action
/// </summary>
public sealed partial class ZombifySelfActionEvent : InstantActionEvent { };
