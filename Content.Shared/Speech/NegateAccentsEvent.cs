namespace Content.Shared.Speech;

public sealed class NegateAccentsEvent : EntityEventArgs
{
    public EntityUid Entity { get; }
    public bool NegateAccents { get; }

    public NegateAccentsEvent(EntityUid entity, bool negateAccents)
    {
        Entity = entity;
        NegateAccents = negateAccents;
    }
}
