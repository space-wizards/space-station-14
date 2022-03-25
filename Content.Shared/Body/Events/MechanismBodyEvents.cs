using Content.Shared.Body.Components;
using Robust.Shared.GameObjects;

namespace Content.Shared.Body.Events
{
    // All of these events are raised on a mechanism entity when added/removed to a body in different
    // ways.

    /// <summary>
    ///     Raised on a mechanism when it is added to a body.
    /// </summary>
    public sealed class MechanismAddedToBodyEvent : EntityEventArgs
    {
        public SharedBodyComponent Body;

        public MechanismAddedToBodyEvent(SharedBodyComponent body)
        {
            Body = body;
        }
    }

    /// <summary>
    ///     Raised on a mechanism when it is added to a body part.
    /// </summary>
    public sealed class MechanismAddedToPartEvent : EntityEventArgs
    {
        public SharedBodyPartComponent Part;

        public MechanismAddedToPartEvent(SharedBodyPartComponent part)
        {
            Part = part;
        }
    }

    /// <summary>
    ///     Raised on a mechanism when it is added to a body part within a body.
    /// </summary>
    public sealed class MechanismAddedToPartInBodyEvent : EntityEventArgs
    {
        public SharedBodyComponent Body;
        public SharedBodyPartComponent Part;

        public MechanismAddedToPartInBodyEvent(SharedBodyComponent body, SharedBodyPartComponent part)
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
        public SharedBodyComponent Old;

        public MechanismRemovedFromBodyEvent(SharedBodyComponent old)
        {
            Old = old;
        }
    }

    /// <summary>
    ///     Raised on a mechanism when it is removed from a body part.
    /// </summary>
    public sealed class MechanismRemovedFromPartEvent : EntityEventArgs
    {
        public SharedBodyPartComponent Old;

        public MechanismRemovedFromPartEvent(SharedBodyPartComponent old)
        {
            Old = old;
        }
    }

    /// <summary>
    ///     Raised on a mechanism when it is removed from a body part within a body.
    /// </summary>
    public sealed class MechanismRemovedFromPartInBodyEvent : EntityEventArgs
    {
        public SharedBodyComponent OldBody;
        public SharedBodyPartComponent OldPart;

        public MechanismRemovedFromPartInBodyEvent(SharedBodyComponent oldBody, SharedBodyPartComponent oldPart)
        {
            OldBody = oldBody;
            OldPart = oldPart;
        }
    }
}
