#nullable enable
using Content.Shared.GameObjects.Components.Body.Mechanism;
using Content.Shared.GameObjects.Components.Body.Part;
using Robust.Shared.GameObjects;

namespace Content.Shared.GameObjects.Components.Body.Behavior
{
    public abstract class MechanismBehaviorComponent : Component, IMechanismBehavior
    {
        public ISharedBodyManager? Body => Part?.Body;

        public IBodyPart? Part => Mechanism?.Part;

        public IMechanism? Mechanism => Owner.GetComponentOrNull<IMechanism>();

        public abstract void Update(float frameTime);

        /// <summary>
        ///     Called when the containing <see cref="IBodyPart"/> is attached to a
        ///     <see cref="BodyManagerComponent"/>.
        ///     For instance, attaching a head to a body will call this on the brain inside.
        /// </summary>
        public void InstalledIntoBody()
        {
            OnInstalledIntoBody();
        }

        /// <summary>
        ///     Called when the parent <see cref="Mechanisms.Mechanism"/> is
        ///     installed into a <see cref="IBodyPart"/>.
        ///     For instance, putting a brain into an empty head.
        /// </summary>
        public void InstalledIntoPart()
        {
            OnInstalledIntoPart();
        }

        /// <summary>
        ///     Called when the containing <see cref="IBodyPart"/> is removed from a
        ///     <see cref="BodyManagerComponent"/>.
        ///     For instance, cutting off ones head will call this on the brain inside.
        /// </summary>
        public void RemovedFromBody(ISharedBodyManager old)
        {
            OnRemovedFromBody(old);
        }

        /// <summary>
        ///     Called when the parent <see cref="Mechanisms.Mechanism"/> is
        ///     removed from a <see cref="IBodyPart"/>.
        ///     For instance, taking a brain out of ones head.
        /// </summary>
        public void RemovedFromPart(IBodyPart old)
        {
            OnRemovedFromPart(old);
        }

        /// <summary>
        ///     Called when the containing <see cref="IBodyPart"/> is attached to a
        ///     <see cref="BodyManagerComponent"/>.
        ///     For instance, attaching a head to a body will call this on the brain inside.
        /// </summary>
        protected virtual void OnInstalledIntoBody() { }

        /// <summary>
        ///     Called when the parent <see cref="Mechanisms.Mechanism"/> is
        ///     installed into a <see cref="IBodyPart"/>.
        ///     For instance, putting a brain into an empty head.
        /// </summary>
        protected virtual void OnInstalledIntoPart() { }

        /// <summary>
        ///     Called when the containing <see cref="IBodyPart"/> is removed from a
        ///     <see cref="BodyManagerComponent"/>.
        ///     For instance, cutting off ones head will call this on the brain inside.
        /// </summary>
        protected virtual void OnRemovedFromBody(ISharedBodyManager old) { }

        /// <summary>
        ///     Called when the parent <see cref="Mechanisms.Mechanism"/> is
        ///     removed from a <see cref="IBodyPart"/>.
        ///     For instance, taking a brain out of ones head.
        /// </summary>
        protected virtual void OnRemovedFromPart(IBodyPart old) { }
    }
}
