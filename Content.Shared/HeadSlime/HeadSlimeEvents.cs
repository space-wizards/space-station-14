using Content.Shared.Actions;

namespace Content.Shared.HeadSlime;

/// <summary>
///     Event that is broadcast whenever an entity is Head Slimed.
///     Used by the HeadSlime gamemode to track total infections.
/// </summary>
[ByRefEvent]
public readonly struct EntityHeadSlimeedEvent
{
    /// <summary>
    ///     The entity that was Head Slimed.
    /// </summary>
    public readonly EntityUid Target;

    public EntityHeadSlimeedEvent(EntityUid target)
    {
        Target = target;
    }
};
