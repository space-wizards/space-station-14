#nullable enable
using Content.Shared.GameObjects.Components.Body.Mechanism;
using Content.Shared.GameObjects.Components.Body.Part;
using Robust.Shared.GameObjects;

namespace Content.Shared.GameObjects.Components.Body.Behavior
{
    public abstract class MechanismComponent : Component, ISharedMechanismBehavior
    {
        public ISharedBodyManager? Body => Part?.Body;

        public ISharedBodyPart? Part => Mechanism?.Part;

        public ISharedMechanism? Mechanism => Owner.GetComponentOrNull<ISharedMechanism>();

        public abstract void Update(float frameTime);

        /// <summary>
        ///     Called when the containing <see cref="ISharedBodyPart"/> is attached to a
        ///     <see cref="BodyManagerComponent"/>.
        ///     For instance, attaching a head to a body will call this on the brain inside.
        /// </summary>
        public void InstalledIntoBody()
        {
            OnInstalledIntoBody();
        }

        /// <summary>
        ///     Called when the parent <see cref="Mechanisms.Mechanism"/> is
        ///     installed into a <see cref="ISharedBodyPart"/>.
        ///     For instance, putting a brain into an empty head.
        /// </summary>
        public void InstalledIntoPart()
        {
            OnInstalledIntoPart();
        }

        /// <summary>
        ///     Called when the containing <see cref="ISharedBodyPart"/> is removed from a
        ///     <see cref="BodyManagerComponent"/>.
        ///     For instance, cutting off ones head will call this on the brain inside.
        /// </summary>
        public void RemovedFromBody(ISharedBodyManager old)
        {
            OnRemovedFromBody(old);
        }

        /// <summary>
        ///     Called when the parent <see cref="Mechanisms.Mechanism"/> is
        ///     removed from a <see cref="ISharedBodyPart"/>.
        ///     For instance, taking a brain out of ones head.
        /// </summary>
        public void RemovedFromPart(ISharedBodyPart old)
        {
            OnRemovedFromPart(old);
        }

        /// <summary>
        ///     Called when the containing <see cref="ISharedBodyPart"/> is attached to a
        ///     <see cref="BodyManagerComponent"/>.
        ///     For instance, attaching a head to a body will call this on the brain inside.
        /// </summary>
        protected virtual void OnInstalledIntoBody() { }

        /// <summary>
        ///     Called when the parent <see cref="Mechanisms.Mechanism"/> is
        ///     installed into a <see cref="ISharedBodyPart"/>.
        ///     For instance, putting a brain into an empty head.
        /// </summary>
        protected virtual void OnInstalledIntoPart() { }

        /// <summary>
        ///     Called when the containing <see cref="ISharedBodyPart"/> is removed from a
        ///     <see cref="BodyManagerComponent"/>.
        ///     For instance, cutting off ones head will call this on the brain inside.
        /// </summary>
        protected virtual void OnRemovedFromBody(ISharedBodyManager old) { }

        /// <summary>
        ///     Called when the parent <see cref="Mechanisms.Mechanism"/> is
        ///     removed from a <see cref="ISharedBodyPart"/>.
        ///     For instance, taking a brain out of ones head.
        /// </summary>
        protected virtual void OnRemovedFromPart(ISharedBodyPart old) { }
    }
}
