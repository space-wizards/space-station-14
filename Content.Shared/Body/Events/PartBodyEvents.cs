namespace Content.Shared.Body.Events;

/// <summary>
///     Raised on a body part and the body when added to a body.
/// </summary>
public sealed class PartAddedToBodyEvent : EntityEventArgs
{
    public readonly EntityUid Body;
    public readonly EntityUid BodyPart;
    public readonly string SlotId;

    public PartAddedToBodyEvent(EntityUid body, EntityUid bodyPart, string slotId)
    {
        Body = body;
        BodyPart = bodyPart;
        SlotId = slotId;
    }
}

/// <summary>
///     Raised on a body part and the body when removed from a body.
/// </summary>
public sealed class PartRemovedFromBodyEvent : EntityEventArgs
{
    public readonly EntityUid Body;
    public readonly EntityUid BodyPart;
    public readonly string SlotId;

    public PartRemovedFromBodyEvent(EntityUid body, EntityUid bodyPart, string slotId)
    {
        Body = body;
        BodyPart = bodyPart;
        SlotId = slotId;
    }
}
