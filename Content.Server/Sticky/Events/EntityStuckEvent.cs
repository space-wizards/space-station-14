namespace Content.Server.Sticky.Events;

/// <summary>
///     Risen on sticky entity when it was stuck to other entity.
/// </summary>
public sealed class EntityStuckEvent : EntityEventArgs
{
    public readonly EntityUid User;

    public EntityStuckEvent(EntityUid user)
    {
        User = user;
    }
}
