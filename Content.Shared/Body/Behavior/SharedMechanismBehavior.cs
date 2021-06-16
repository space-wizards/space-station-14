#nullable enable
using Content.Shared.Body.Components;
using Content.Shared.Body.Mechanism;
using Content.Shared.Body.Part;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Body.Behavior
{
    /// <summary>
    ///     Gives functionality to a <see cref="SharedMechanismComponent"/> when added to it.
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
        ///     Called when this <see cref="SharedMechanismBehavior"/> is added to a
        ///     <see cref="SharedMechanismComponent"/>, during <see cref="IComponent.Initialize"/>.
        ///     If it is added after component initialization,
        ///     it is called immediately.
        /// </summary>
        /// <param name="parent">
        ///     The mechanism that owns this <see cref="SharedMechanismBehavior"/>.
        /// </param>
        public abstract void Initialize(SharedMechanismComponent parent);

        /// <summary>
        ///     Called when this <see cref="SharedMechanismBehavior"/> is added to a
        ///     <see cref="SharedMechanismComponent"/>, during <see cref="Component.Startup"/>.
        ///     If it is added after component startup, it is called immediately.
        /// </summary>
        public abstract void Startup();

        /// <summary>
        ///     Runs an update cycle on this <see cref="SharedMechanismBehavior"/>.
        /// </summary>
        /// <param name="frameTime">
        ///     The amount of seconds that passed since the last update.
        /// </param>
        public abstract void Update(float frameTime);

        /// <summary>
        ///     Called when the containing <see cref="SharedBodyPartComponent"/> is attached to a
        ///     <see cref="Content.Shared.Body.Components.SharedBodyComponent"/>.
        ///     For instance, attaching a head with a brain inside to a body.
        ///     DO NOT CALL THIS DIRECTLY FROM OUTSIDE BODY SYSTEM CODE!
        /// </summary>
        /// <param name="body">
        ///     The body that the containing <see cref="SharedMechanismComponent"/> was added to.
        /// </param>
        public abstract void AddedToBody(SharedBodyComponent body);

        /// <summary>
        ///     Called when the parent <see cref="SharedMechanismComponent"/> is
        ///     added into a <see cref="SharedBodyPartComponent"/> that is not attached to a
        ///     <see cref="Content.Shared.Body.Components.SharedBodyComponent"/>.
        ///     For instance, adding a brain to a dismembered head.
        ///     DO NOT CALL THIS DIRECTLY FROM OUTSIDE BODY SYSTEM CODE!
        /// </summary>
        /// <param name="part">
        ///     The part that the containing <see cref="SharedMechanismComponent"/> was added to.
        /// </param>
        public abstract void AddedToPart(SharedBodyPartComponent part);

        /// <summary>
        ///     Called when the parent <see cref="SharedMechanismComponent"/> is added to a
        ///     <see cref="SharedBodyPartComponent"/> that is attached to a <see cref="Content.Shared.Body.Components.SharedBodyComponent"/>.
        ///     For instance, adding a brain to a head that is attached to a body.
        ///     DO NOT CALL THIS DIRECTLY FROM OUTSIDE BODY SYSTEM CODE!
        /// </summary>
        /// <param name="body">
        ///     The body that the containing <see cref="SharedMechanismComponent"/> was added to.
        /// </param>
        /// <param name="part">
        ///     The part that the containing <see cref="SharedMechanismComponent"/> was added to.
        /// </param>
        public abstract void AddedToPartInBody(SharedBodyComponent body, SharedBodyPartComponent part);

        /// <summary>
        ///     Called when the parent <see cref="SharedBodyPartComponent"/> is removed from a
        ///     <see cref="Content.Shared.Body.Components.SharedBodyComponent"/>.
        ///     For instance, removing a head with a brain inside from a body.
        ///     DO NOT CALL THIS DIRECTLY FROM OUTSIDE BODY SYSTEM CODE!
        /// </summary>
        /// <param name="old">
        ///     The body that the containing <see cref="SharedMechanismComponent"/> was removed from.
        /// </param>
        public abstract void RemovedFromBody(SharedBodyComponent old);

        /// <summary>
        ///     Called when the parent <see cref="SharedMechanismComponent"/> is
        ///     removed from a <see cref="SharedBodyPartComponent"/> that is not attached to a
        ///     <see cref="Content.Shared.Body.Components.SharedBodyComponent"/>.
        ///     For instance, removing a brain from a dismembered head.
        ///     DO NOT CALL THIS DIRECTLY FROM OUTSIDE BODY SYSTEM CODE!
        /// </summary>
        /// <param name="old">
        ///     The part that the containing <see cref="SharedMechanismComponent"/> was removed from.
        /// </param>
        public abstract void RemovedFromPart(SharedBodyPartComponent old);

        /// <summary>
        ///     Called when the parent <see cref="SharedMechanismComponent"/> is removed from a
        ///     <see cref="SharedBodyPartComponent"/> that is attached to a <see cref="Content.Shared.Body.Components.SharedBodyComponent"/>.
        ///     For instance, removing a brain from a head that is attached to a body.
        ///     DO NOT CALL THIS DIRECTLY FROM OUTSIDE BODY SYSTEM CODE!
        /// </summary>
        /// <param name="oldBody">
        ///     The body that the containing <see cref="SharedMechanismComponent"/> was removed from.
        /// </param>
        /// <param name="oldPart">
        ///     The part that the containing <see cref="SharedMechanismComponent"/> was removed from.
        /// </param>
        public abstract void RemovedFromPartInBody(SharedBodyComponent oldBody, SharedBodyPartComponent oldPart);
    }
}
