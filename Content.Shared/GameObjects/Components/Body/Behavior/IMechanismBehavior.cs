#nullable enable
using Content.Shared.GameObjects.Components.Body.Mechanism;
using Content.Shared.GameObjects.Components.Body.Part;

namespace Content.Shared.GameObjects.Components.Body.Behavior
{
    public interface IMechanismBehavior : IHasBody
    {
        IBodyPart? Part { get; }

        /// <summary>
        ///     Upward reference to the parent <see cref="IMechanism"/> that this
        ///     behavior is attached to.
        /// </summary>
        IMechanism? Mechanism { get; }

        void Update(float frameTime);

        /// <summary>
        ///     Called when the containing <see cref="IBodyPart"/> is attached to a
        ///     <see cref="IBody"/>.
        ///     For instance, attaching a head with a brain inside to a body.
        ///     DO NOT CALL THIS DIRECTLY FROM OUTSIDE BODY SYSTEM CODE!
        /// </summary>
        void AddedToBody();

        /// <summary>
        ///     Called when the parent <see cref="IMechanism"/> is
        ///     added into a <see cref="IBodyPart"/> that is not attached to a
        ///     <see cref="IBody"/>.
        ///     For instance, adding a brain to a dismembered head.
        ///     DO NOT CALL THIS DIRECTLY FROM OUTSIDE BODY SYSTEM CODE!
        /// </summary>
        void AddedToPart();

        /// <summary>
        ///     Called when the parent <see cref="IBodyPart"/> is removed from a
        ///     <see cref="IBody"/>.
        ///     For instance, removing a head with a brain inside from a body.
        ///     DO NOT CALL THIS DIRECTLY FROM OUTSIDE BODY SYSTEM CODE!
        /// </summary>
        void RemovedFromBody(IBody old);

        /// <summary>
        ///     Called when the parent <see cref="IMechanism"/> is
        ///     removed from a <see cref="IBodyPart"/> that is not attached to a
        ///     <see cref="IBody"/>.
        ///     For instance, removing a brain from a dismembered head.
        ///     DO NOT CALL THIS DIRECTLY FROM OUTSIDE BODY SYSTEM CODE!
        /// </summary>
        void RemovedFromPart(IBodyPart old);

        /// <summary>
        ///     Called when the parent <see cref="IMechanism"/> is added to a
        ///     <see cref="IBodyPart"/> that is attached to a <see cref="IBody"/>.
        ///     For instance, adding a brain to a head that is attached to a body.
        ///     DO NOT CALL THIS DIRECTLY FROM OUTSIDE BODY SYSTEM CODE!
        /// </summary>
        void AddedToPartInBody();

        /// <summary>
        ///     Called when the parent <see cref="IMechanism"/> is removed from a
        ///     <see cref="IBodyPart"/> that is attached to a <see cref="IBody"/>.
        ///     For instance, removing a brain from a head that is attached to a body.
        ///     DO NOT CALL THIS DIRECTLY FROM OUTSIDE BODY SYSTEM CODE!
        /// </summary>
        void RemovedFromPartInBody(IBody? oldBody, IBodyPart? oldPart);
    }
}
