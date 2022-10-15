using Content.Shared.Body.Components;

namespace Content.Shared.Body.Events
{
    // All of these events are raised on a mechanism entity when added/removed to a body in different
    // ways.

    /// <summary>
    ///     Raised on a mechanism when it is added to a body.
    /// </summary>
    public sealed class AddedToBodyEvent : EntityEventArgs
    {
        public BodyComponent Body;

        public AddedToBodyEvent(BodyComponent body)
        {
            Body = body;
        }
    }

    /// <summary>
    ///     Raised on a mechanism when it is added to a body part.
    /// </summary>
    public sealed class AddedToPartEvent : EntityEventArgs
    {
        public BodyComponent Part;

        public AddedToPartEvent(BodyComponent part)
        {
            Part = part;
        }
    }

    /// <summary>
    ///     Raised on a mechanism when it is added to a body part within a body.
    /// </summary>
    public sealed class AddedToPartInBodyEvent : EntityEventArgs
    {
        public BodyComponent Body;
        public BodyComponent Part;

        public AddedToPartInBodyEvent(BodyComponent body, BodyComponent part)
        {
            Body = body;
            Part = part;
        }
    }

    /// <summary>
    ///     Raised on a mechanism when it is removed from a body.
    /// </summary>
    public sealed class RemovedFromBodyEvent : EntityEventArgs
    {
        public BodyComponent Old;

        public RemovedFromBodyEvent(BodyComponent old)
        {
            Old = old;
        }
    }

    /// <summary>
    ///     Raised on a mechanism when it is removed from a body part.
    /// </summary>
    public sealed class RemovedFromPartEvent : EntityEventArgs
    {
        public BodyComponent Old;

        public RemovedFromPartEvent(BodyComponent old)
        {
            Old = old;
        }
    }

    /// <summary>
    ///     Raised on a mechanism when it is removed from a body part within a body.
    /// </summary>
    public sealed class RemovedFromPartInBodyEvent : EntityEventArgs
    {
        public BodyComponent OldBody;
        public BodyComponent OldPart;

        public RemovedFromPartInBodyEvent(BodyComponent oldBody, BodyComponent oldPart)
        {
            OldBody = oldBody;
            OldPart = oldPart;
        }
    }
}
