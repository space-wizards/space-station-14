using Content.Shared.Body.Components;

namespace Content.Shared.Body.Events
{
    // All of these events are raised on a mechanism entity when added/removed to a body in different
    // ways.

    /// <summary>
    ///     Raised on a mechanism when it is added to a body.
    /// </summary>
    public sealed class MechanismAddedToBodyEvent : EntityEventArgs
    {
        public EntityUid Body;

        public MechanismAddedToBodyEvent(EntityUid body)
        {
            Body = body;
        }
    }

    /// <summary>
    ///     Raised on a mechanism when it is added to a body part.
    /// </summary>
    public sealed class MechanismAddedToPartEvent : EntityEventArgs
    {
        public EntityUid Part;

        public MechanismAddedToPartEvent(EntityUid part)
        {
            Part = part;
        }
    }

    /// <summary>
    ///     Raised on a mechanism when it is added to a body part within a body.
    /// </summary>
    public sealed class MechanismAddedToPartInBodyEvent : EntityEventArgs
    {
        public EntityUid Body;
        public EntityUid Part;

        public MechanismAddedToPartInBodyEvent(EntityUid body, EntityUid part)
        {
            Body = body;
            Part = part;
        }
    }

    /// <summary>
    ///     Raised on a mechanism when it is removed from a body.
    /// </summary>
    public sealed class MechanismRemovedFromBodyEvent : EntityEventArgs
    {
        public EntityUid Body;

        public MechanismRemovedFromBodyEvent(EntityUid body)
        {
            Body = body;
        }
    }

    /// <summary>
    ///     Raised on a mechanism when it is removed from a body part.
    /// </summary>
    public sealed class MechanismRemovedFromPartEvent : EntityEventArgs
    {
        public EntityUid Part;

        public MechanismRemovedFromPartEvent(EntityUid part)
        {
            Part = part;
        }
    }

    /// <summary>
    ///     Raised on a mechanism when it is removed from a body part within a body.
    /// </summary>
    public sealed class MechanismRemovedFromPartInBodyEvent : EntityEventArgs
    {
        public EntityUid Body;
        public EntityUid Part;

        public MechanismRemovedFromPartInBodyEvent(EntityUid body, EntityUid part)
        {
            Body = body;
            Part = part;
        }
    }
}
