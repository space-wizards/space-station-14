using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Body.Behavior
{
    /// <summary>
    ///     Gives functionality to a mechanism when added to it.
    /// </summary>
    [ImplicitDataDefinitionForInheritors]
    public abstract class SharedMechanismBehavior
    {
        public abstract SharedBodyComponent? Body { get; }

        public abstract SharedBodyPartComponent? Part { get; }

        public abstract SharedMechanismComponent Parent { get; }

        /// <summary>
        ///     The entity that owns <see cref="Parent"/>.
        ///     For the entity owning the body that this mechanism may be in,
        ///     see <see cref="Body"/>'s <see cref="SharedBodyComponent.Owner"/>.
        /// </summary>
        public abstract IEntity Owner { get; }

        /// <summary>
        ///     Called when this behavior is added to a mechanism, during <see cref="IComponent.Initialize"/>.
        ///     If it is added after component initialization,
        ///     it is called immediately.
        /// </summary>
        /// <param name="parent">
        ///     The mechanism that owns this behavior.
        /// </param>
        public abstract void Initialize(SharedMechanismComponent parent);

        /// <summary>
        ///     Called when this behavior is added to a mechanism, during <see cref="Component.Startup"/>.
        ///     If it is added after component startup, it is called immediately.
        /// </summary>
        public abstract void Startup();

        /// <summary>
        ///     Runs an update cycle on this behavior.
        /// </summary>
        /// <param name="frameTime">
        ///     The amount of seconds that passed since the last update.
        /// </param>
        public abstract void Update(float frameTime);

        /// <summary>
        ///     Called when the containing part is attached to a body.
        ///     For instance, attaching a head with a brain inside to a body.
        ///     DO NOT CALL THIS DIRECTLY FROM OUTSIDE BODY SYSTEM CODE!
        /// </summary>
        /// <param name="body">
        ///     The body that the containing mechanism was added to.
        /// </param>
        public abstract void AddedToBody(SharedBodyComponent body);

        /// <summary>
        ///     Called when the parent mechanism is added into a part that is not attached to a body.
        ///     For instance, adding a brain to a dismembered head.
        ///     DO NOT CALL THIS DIRECTLY FROM OUTSIDE BODY SYSTEM CODE!
        /// </summary>
        /// <param name="part">
        ///     The part that the containing mechanism was added to.
        /// </param>
        public abstract void AddedToPart(SharedBodyPartComponent part);

        /// <summary>
        ///     Called when the parent mechanism is added to a part that is attached to a body.
        ///     For instance, adding a brain to a head that is attached to a body.
        ///     DO NOT CALL THIS DIRECTLY FROM OUTSIDE BODY SYSTEM CODE!
        /// </summary>
        /// <param name="body">
        ///     The body that the containing mechanism was added to.
        /// </param>
        /// <param name="part">
        ///     The part that the containing mechanism was added to.
        /// </param>
        public abstract void AddedToPartInBody(SharedBodyComponent body, SharedBodyPartComponent part);

        /// <summary>
        ///     Called when the parent part is removed from a body.
        ///     For instance, removing a head with a brain inside from a body.
        ///     DO NOT CALL THIS DIRECTLY FROM OUTSIDE BODY SYSTEM CODE!
        /// </summary>
        /// <param name="old">
        ///     The body that the containing mechanism was removed from.
        /// </param>
        public abstract void RemovedFromBody(SharedBodyComponent old);

        /// <summary>
        ///     Called when the parent mechanism is removed from a part that is not attached to a body.
        ///     For instance, removing a brain from a dismembered head.
        ///     DO NOT CALL THIS DIRECTLY FROM OUTSIDE BODY SYSTEM CODE!
        /// </summary>
        /// <param name="old">
        ///     The part that the containing mechanism was removed from.
        /// </param>
        public abstract void RemovedFromPart(SharedBodyPartComponent old);

        /// <summary>
        ///     Called when the parent mechanism is removed from a part that is attached to a body.
        ///     For instance, removing a brain from a head that is attached to a body.
        ///     DO NOT CALL THIS DIRECTLY FROM OUTSIDE BODY SYSTEM CODE!
        /// </summary>
        /// <param name="oldBody">
        ///     The body that the containing mechanism was removed from.
        /// </param>
        /// <param name="oldPart">
        ///     The part that the containing mechanism was removed from.
        /// </param>
        public abstract void RemovedFromPartInBody(SharedBodyComponent oldBody, SharedBodyPartComponent oldPart);
    }
}
