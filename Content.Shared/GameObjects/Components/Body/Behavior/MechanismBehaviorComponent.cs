#nullable enable
using Content.Shared.GameObjects.Components.Body.Mechanism;
using Content.Shared.GameObjects.Components.Body.Part;
using Robust.Shared.GameObjects;

namespace Content.Shared.GameObjects.Components.Body.Behavior
{
    public abstract class MechanismBehaviorComponent : Component, IMechanismBehavior
    {
        public IBody? Body => Part?.Body;

        public IBodyPart? Part => Mechanism?.Part;

        public IMechanism? Mechanism => Owner.GetComponentOrNull<IMechanism>();

        public abstract void Update(float frameTime);

        /// <summary>
        ///     Called when the containing <see cref="IBodyPart"/> is attached to a
        ///     <see cref="IBody"/>.
        ///     For instance, attaching a head to a body will call this on the brain inside.
        /// </summary>
        public void AddedToBody()
        {
            OnAddedToBody();
        }

        /// <summary>
        ///     Called when the parent <see cref="Mechanism"/> is
        ///     added into a <see cref="IBodyPart"/>.
        ///     For instance, putting a brain into an empty head.
        /// </summary>
        public void AddedToPart()
        {
            OnAddedToPart();
        }

        /// <summary>
        ///     Called when the containing <see cref="IBodyPart"/> is removed from a
        ///     <see cref="IBody"/>.
        ///     For instance, cutting off ones head will call this on the brain inside.
        /// </summary>
        public void RemovedFromBody(IBody old)
        {
            OnRemovedFromBody(old);
        }

        /// <summary>
        ///     Called when the parent <see cref="Mechanism"/> is
        ///     removed from a <see cref="IBodyPart"/>.
        ///     For instance, taking a brain out of ones head.
        /// </summary>
        public void RemovedFromPart(IBodyPart old)
        {
            OnRemovedFromPart(old);
        }

        /// <summary>
        ///     Called when the containing <see cref="IBodyPart"/> is attached to a
        ///     <see cref="IBody"/>.
        ///     For instance, attaching a head to a body will call this on the brain inside.
        /// </summary>
        protected virtual void OnAddedToBody() { }

        /// <summary>
        ///     Called when the parent <see cref="Mechanism"/> is
        ///     added into a <see cref="IBodyPart"/>.
        ///     For instance, putting a brain into an empty head.
        /// </summary>
        protected virtual void OnAddedToPart() { }

        /// <summary>
        ///     Called when the containing <see cref="IBodyPart"/> is removed from a
        ///     <see cref="IBody"/>.
        ///     For instance, cutting off ones head will call this on the brain inside.
        /// </summary>
        protected virtual void OnRemovedFromBody(IBody old) { }

        /// <summary>
        ///     Called when the parent <see cref="Mechanism"/> is
        ///     removed from a <see cref="IBodyPart"/>.
        ///     For instance, taking a brain out of ones head.
        /// </summary>
        protected virtual void OnRemovedFromPart(IBodyPart old) { }
    }
}
