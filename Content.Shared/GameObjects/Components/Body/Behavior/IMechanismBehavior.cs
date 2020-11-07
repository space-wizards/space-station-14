#nullable enable
using Content.Shared.GameObjects.Components.Body.Mechanism;
using Content.Shared.GameObjects.Components.Body.Part;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;

namespace Content.Shared.GameObjects.Components.Body.Behavior
{
    public interface IMechanismBehavior : IExposeData
    {
        IBody? Body { get; }

        IBodyPart? Part { get; }

        /// <summary>
        ///     Upward reference to the parent <see cref="IMechanism"/> that this
        ///     behavior is attached to.
        /// </summary>
        IMechanism Parent { get; }

        /// <summary>
        ///     The entity that owns <see cref="Parent"/>.
        ///     For the entity owning the body that this mechanism may be in,
        ///     see <see cref="IBody.Owner"/>
        /// </summary>
        IEntity Owner { get; }

        void Initialize(IMechanism parent);

        void Startup();

        void Update(float frameTime);

        /// <summary>
        ///     Called when the containing <see cref="IBodyPart"/> is attached to a
        ///     <see cref="IBody"/>.
        ///     For instance, attaching a head with a brain inside to a body.
        ///     DO NOT CALL THIS DIRECTLY FROM OUTSIDE BODY SYSTEM CODE!
        /// </summary>
        /// <param name="body">
        ///     The body that the containing <see cref="IMechanism"/> was added to.
        /// </param>
        void AddedToBody(IBody body);

        /// <summary>
        ///     Called when the parent <see cref="IMechanism"/> is
        ///     added into a <see cref="IBodyPart"/> that is not attached to a
        ///     <see cref="IBody"/>.
        ///     For instance, adding a brain to a dismembered head.
        ///     DO NOT CALL THIS DIRECTLY FROM OUTSIDE BODY SYSTEM CODE!
        /// </summary>
        /// <param name="part">
        ///     The part that the containing <see cref="IMechanism"/> was added to.
        /// </param>
        void AddedToPart(IBodyPart part);

        /// <summary>
        ///     Called when the parent <see cref="IMechanism"/> is added to a
        ///     <see cref="IBodyPart"/> that is attached to a <see cref="IBody"/>.
        ///     For instance, adding a brain to a head that is attached to a body.
        ///     DO NOT CALL THIS DIRECTLY FROM OUTSIDE BODY SYSTEM CODE!
        /// </summary>
        /// <param name="body">
        ///     The body that the containing <see cref="IMechanism"/> was added to.
        /// </param>
        /// <param name="part">
        ///     The part that the containing <see cref="IMechanism"/> was added to.
        /// </param>
        void AddedToPartInBody(IBody body, IBodyPart part);

        /// <summary>
        ///     Called when the parent <see cref="IBodyPart"/> is removed from a
        ///     <see cref="IBody"/>.
        ///     For instance, removing a head with a brain inside from a body.
        ///     DO NOT CALL THIS DIRECTLY FROM OUTSIDE BODY SYSTEM CODE!
        /// </summary>
        /// <param name="old">
        ///     The body that the containing <see cref="IMechanism"/> was removed from.
        /// </param>
        void RemovedFromBody(IBody old);

        /// <summary>
        ///     Called when the parent <see cref="IMechanism"/> is
        ///     removed from a <see cref="IBodyPart"/> that is not attached to a
        ///     <see cref="IBody"/>.
        ///     For instance, removing a brain from a dismembered head.
        ///     DO NOT CALL THIS DIRECTLY FROM OUTSIDE BODY SYSTEM CODE!
        /// </summary>
        /// <param name="old">
        ///     The part that the containing <see cref="IMechanism"/> was removed from.
        /// </param>
        void RemovedFromPart(IBodyPart old);

        /// <summary>
        ///     Called when the parent <see cref="IMechanism"/> is removed from a
        ///     <see cref="IBodyPart"/> that is attached to a <see cref="IBody"/>.
        ///     For instance, removing a brain from a head that is attached to a body.
        ///     DO NOT CALL THIS DIRECTLY FROM OUTSIDE BODY SYSTEM CODE!
        /// </summary>
        /// <param name="oldBody">
        ///     The body that the containing <see cref="IMechanism"/> was removed from.
        /// </param>
        /// <param name="oldPart">
        ///     The part that the containing <see cref="IMechanism"/> was removed from.
        /// </param>
        void RemovedFromPartInBody(IBody oldBody, IBodyPart oldPart);
    }
}
