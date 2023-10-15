namespace Content.Shared.UserInterface;

public sealed class AfterActivatableUIOpenEvent : EntityEventArgs
{
    public EntityUid User { get; }
    public readonly IPlayerSession Session;

    public AfterActivatableUIOpenEvent(EntityUid who, IPlayerSession session)
    {
        User = who;
        Session = session;
    }
}