namespace Content.Shared.Climbing.Events;

public sealed class AttemptClimbEvent : CancellableEntityEventArgs
{
    public EntityUid User;
    public EntityUid Climber;
    public EntityUid Climbable;

    public AttemptClimbEvent(EntityUid user, EntityUid climber, EntityUid climbable)
    {
        User = user;
        Climber = climber;
        Climbable = climbable;
    }
}
