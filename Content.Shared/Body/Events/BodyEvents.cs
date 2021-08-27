using Content.Shared.Body.Components;
using Robust.Shared.GameObjects;

namespace Content.Shared.Body.Events
{
    /// <summary>
    ///     Raised on an entity when a body part is added to it.
    /// </summary>
    /// <remarks>Replaces <see cref="Content.Shared.Body.Part.IBodyPartAdded"/></remarks>
    public class BodyPartAddedEvent : EntityEventArgs
    {
        public string Slot;
        public SharedBodyPartComponent Part;

        public BodyPartAddedEvent(string slot, SharedBodyPartComponent part)
        {
            Slot = slot;
            Part = part;
        }
    }

    /// <summary>
    ///     Raised on parts and mechanisms when they are added to a body.
    /// </summary>
    /// <remarks>These all replace old events in <see cref="MechanismBehavior"/></remarks>
    public class AddedToBodyEvent : EntityEventArgs
    {
        public SharedBodyComponent Body;

        public AddedToBodyEvent(SharedBodyComponent body)
        {
            Body = body;
        }
    }

    /// <summary>
    ///     Raised on mechanisms when they are added to a body part outside of a body.
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
    ///     Raised on a mechanism when it is added to a body part inside of a body.
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
    ///     Raised on parts or mechanisms when they are removed from a body.
    /// </summary>
    public class RemovedFromBodyEvent : EntityEventArgs
    {
        public SharedBodyComponent Body;

        public RemovedFromBodyEvent(SharedBodyComponent body)
        {
            Body = body;
        }
    }

    /// <summary>
    ///     Raised on mechanisms when removed from a part outside of a body.
    /// </summary>
    public class RemovedFromPartEvent : EntityEventArgs
    {
        public SharedBodyPartComponent Part;

        public RemovedFromPartEvent(SharedBodyPartComponent part)
        {
            Part = part;
        }
    }

    /// <summary>
    ///     Raised on mechanisms when removed from a part inside of a body.
    /// </summary>
    public class RemovedFromPartInBodyEvent : EntityEventArgs
    {
        public SharedBodyComponent Body;
        public SharedBodyPartComponent Part;

        public RemovedFromPartInBodyEvent(SharedBodyComponent body, SharedBodyPartComponent part)
        {
            Body = body;
            Part = part;
        }
    }
}
