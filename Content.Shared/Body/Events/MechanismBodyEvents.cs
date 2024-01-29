namespace Content.Shared.Body.Events
{
    // All of these events are raised on a mechanism entity when added/removed to a body in different
    // ways.

    /// <summary>
    ///     Raised on a mechanism when it is added to a body part.
    /// </summary>
    public sealed class AddedToPartEvent : EntityEventArgs
    {
        public EntityUid Part;

        public AddedToPartEvent(EntityUid part)
        {
            Part = part;
        }
    }

    /// <summary>
    ///     Raised on a mechanism when it is added to a body part within a body.
    /// </summary>
    public sealed class AddedToPartInBodyEvent : EntityEventArgs
    {
        public EntityUid Body;
        public EntityUid Part;

        public AddedToPartInBodyEvent(EntityUid body, EntityUid part)
        {
            Body = body;
            Part = part;
        }
    }

    /// <summary>
    ///     Raised on a mechanism when it is removed from a body part.
    /// </summary>
    public sealed class RemovedFromPartEvent : EntityEventArgs
    {
        public EntityUid OldPart;

        public RemovedFromPartEvent(EntityUid oldPart)
        {
            OldPart = oldPart;
        }
    }

    /// <summary>
    ///     Raised on a mechanism when it is removed from a body part within a body.
    /// </summary>
    public sealed class RemovedFromPartInBodyEvent : EntityEventArgs
    {
        public EntityUid OldBody;
        public EntityUid OldPart;

        public RemovedFromPartInBodyEvent(EntityUid oldBody, EntityUid oldPart)
        {
            OldBody = oldBody;
            OldPart = oldPart;
        }
    }
}
