namespace Content.Shared.Interaction.Events;

/// <summary>
/// Raised on directed a weapon when being used in a melee attack.
/// </summary>
[ByRefEvent]
public struct MeleeAttackAttemptEvent
{
    public bool Cancelled = false;
    public readonly EntityUid User;

    public MeleeAttackAttemptEvent(EntityUid user)
    {
        User = user;
    }
}
