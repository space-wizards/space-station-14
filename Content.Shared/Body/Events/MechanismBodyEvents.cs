using Content.Shared.Body.Components;
using Robust.Shared.GameObjects;

namespace Content.Shared.Body.Events
{
    // All of these events are raised on a mechanism entity when added/removed to a body in different
    // ways.

    /// <summary>
    ///     Raised on a mechanism when it is added to a body.
    /// </summary>
    public class AddedToBodyEvent : EntityEventArgs
    {
        public SharedBodyComponent Body;

        public AddedToBodyEvent(SharedBodyComponent body)
        {
            Body = body;
        }
    }

    /// <summary>
    ///     Raised on a mechanism when it is added to a body part.
    /// </summary>
    public class AddedToPartEvent : EntityEventArgs
    {
        public SharedBodyPartComponent Part;

        public AddedToPartEvent(SharedBodyPartComponent part)
        {
            Part = part;
        }
    }

    /// <summary>
    ///     Raised on a mechanism when it is added to a body part within a body.
    /// </summary>
    public class AddedToPartInBodyEvent : EntityEventArgs
    {
        public SharedBodyComponent Body;
        public SharedBodyPartComponent Part;

        public AddedToPartInBodyEvent(SharedBodyComponent body, SharedBodyPartComponent part)
        {
            Body = body;
            Part = part;
        }
    }

    /// <summary>
    ///     Raised on a mechanism when it is removed from a body.
    /// </summary>
    public class RemovedFromBodyEvent : EntityEventArgs
    {
        public SharedBodyComponent Old;

        public RemovedFromBodyEvent(SharedBodyComponent old)
        {
            Old = old;
        }
    }

    /// <summary>
    ///     Raised on a mechanism when it is removed from a body part.
    /// </summary>
    public class RemovedFromPartEvent : EntityEventArgs
    {
        public SharedBodyPartComponent Old;

        public RemovedFromPartEvent(SharedBodyPartComponent old)
        {
            Old = old;
        }
    }

    /// <summary>
    ///     Raised on a mechanism when it is removed from a body part within a body.
    /// </summary>
    public class RemovedFromPartInBodyEvent : EntityEventArgs
    {
        public SharedBodyComponent OldBody;
        public SharedBodyPartComponent OldPart;

        public RemovedFromPartInBodyEvent(SharedBodyComponent oldBody, SharedBodyPartComponent oldPart)
        {
            OldBody = oldBody;
            OldPart = oldPart;
        }
    }
}
