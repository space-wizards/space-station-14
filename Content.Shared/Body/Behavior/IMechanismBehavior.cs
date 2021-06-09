#nullable enable
using Content.Shared.Body.Components;
using Content.Shared.Body.Mechanism;
using Content.Shared.Body.Part;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Body.Behavior
{
    /// <summary>
    ///     Gives functionality to a <see cref="IMechanism"/> when added to it.
    /// </summary>
    [ImplicitDataDefinitionForInheritors]
    public interface IMechanismBehavior
    {
        /// <summary>
        ///     The body that owns the <see cref="IBodyPart"/> in which the
        ///     <see cref="IMechanism"/> that owns this
        ///     <see cref="IMechanismBehavior"/> is in.
        /// </summary>
        IBody? Body { get; }

        /// <summary>
        ///     The part in which the <see cref="IMechanism"/> that owns this
        ///     <see cref="IMechanismBehavior"/> is in.
        /// </summary>
        IBodyPart? Part { get; }

        /// <summary>
        ///     Upward reference to the parent <see cref="IMechanism"/> that this
        ///     behavior is attached to.
        /// </summary>
        IMechanism Parent { get; }

        /// <summary>
        ///     The entity that owns <see cref="Parent"/>.
        ///     For the entity owning the body that this mechanism may be in,
        ///     see <see cref="Body"/>'s <see cref="IBody.Owner"/>.
        /// </summary>
        IEntity Owner { get; }

        /// <summary>
        ///     Called when this <see cref="IMechanismBehavior"/> is added to a
        ///     <see cref="IMechanism"/>, during <see cref="IComponent.Initialize"/>.
        ///     If it is added after component initialization,
        ///     it is called immediately.
        /// </summary>
        /// <param name="parent">
        ///     The mechanism that owns this <see cref="IMechanismBehavior"/>.
        /// </param>
        void Initialize(IMechanism parent);

        /// <summary>
        ///     Called when this <see cref="IMechanismBehavior"/> is added to a
        ///     <see cref="IMechanism"/>, during <see cref="IComponent.Startup"/>.
        ///     If it is added after component startup, it is called immediately.
        /// </summary>
        void Startup();

        /// <summary>
        ///     Runs an update cycle on this <see cref="IMechanismBehavior"/>.
        /// </summary>
        /// <param name="frameTime">
        ///     The amount of seconds that passed since the last update.
        /// </param>
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
