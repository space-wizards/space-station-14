#nullable enable
using System;
using Content.Server.GameObjects.Components.Body;
using Content.Server.GameObjects.Components.Metabolism;

namespace Content.Server.Body.Mechanisms.Behaviors
{
    /// <summary>
    ///     The behaviors a mechanism performs.
    /// </summary>
    public abstract class MechanismBehavior
    {
        private bool Initialized { get; set; }

        private bool Removed { get; set; }

        /// <summary>
        ///     The network, if any, that this behavior forms when its mechanism is
        ///     added and destroys when its mechanism is removed.
        /// </summary>
        protected virtual Type? Network { get; } = null;

        /// <summary>
        ///     Upward reference to the parent <see cref="Mechanisms.Mechanism"/> that this
        ///     behavior is attached to.
        /// </summary>
        protected Mechanism Mechanism { get; private set; } = null!;

        /// <summary>
        ///     Called by a <see cref="Mechanism"/> to initialize this behavior.
        /// </summary>
        /// <param name="mechanism">The mechanism that owns this behavior.</param>
        /// <exception cref="InvalidOperationException">
        ///     If the mechanism has already been initialized.
        /// </exception>
        public void Initialize(Mechanism mechanism)
        {
            if (Initialized)
            {
                throw new InvalidOperationException("This mechanism has already been initialized.");
            }

            Mechanism = mechanism;

            Initialize();

            if (Mechanism.Body != null)
            {
                OnInstalledIntoBody();
            }

            if (Mechanism.Part != null)
            {
                OnInstalledIntoPart();
            }

            Initialized = true;
        }

        /// <summary>
        ///     Called when a behavior is removed from a <see cref="Mechanism"/>.
        /// </summary>
        public void Remove()
        {
            OnRemove();
            TryRemoveNetwork(Mechanism.Body);

            Mechanism = null!;
            Removed = true;
        }

        /// <summary>
        ///     Called when the containing <see cref="IBodyPart"/> is attached to a
        ///     <see cref="BodyManagerComponent"/>.
        ///     For instance, attaching a head to a body will call this on the brain inside.
        /// </summary>
        public void InstalledIntoBody()
        {
            TryAddNetwork();
            OnInstalledIntoBody();
        }

        /// <summary>
        ///     Called when the parent <see cref="Mechanisms.Mechanism"/> is
        ///     installed into a <see cref="IBodyPart"/>.
        ///     For instance, putting a brain into an empty head.
        /// </summary>
        public void InstalledIntoPart()
        {
            TryAddNetwork();
            OnInstalledIntoPart();
        }

        /// <summary>
        ///     Called when the containing <see cref="IBodyPart"/> is removed from a
        ///     <see cref="BodyManagerComponent"/>.
        ///     For instance, cutting off ones head will call this on the brain inside.
        /// </summary>
        public void RemovedFromBody(IBodyManagerComponent old)
        {
            OnRemovedFromBody(old);
            TryRemoveNetwork(old);
        }

        /// <summary>
        ///     Called when the parent <see cref="Mechanisms.Mechanism"/> is
        ///     removed from a <see cref="IBodyPart"/>.
        ///     For instance, taking a brain out of ones head.
        /// </summary>
        public void RemovedFromPart(IBodyPart old)
        {
            OnRemovedFromPart(old);
            TryRemoveNetwork(old.Body);
        }

        private void TryAddNetwork()
        {
            if (Network != null)
            {
                Mechanism.Body?.EnsureNetwork(Network);
            }
        }

        private void TryRemoveNetwork(IBodyManagerComponent? body)
        {
            if (Network != null)
            {
                body?.RemoveNetwork(Network);
            }
        }

        /// <summary>
        ///     Called by <see cref="Initialize"/> when this behavior is first initialized.
        /// </summary>
        protected virtual void Initialize() { }

        protected virtual void OnRemove() { }

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
        protected virtual void OnRemovedFromBody(IBodyManagerComponent old) { }

        /// <summary>
        ///     Called when the parent <see cref="Mechanisms.Mechanism"/> is
        ///     removed from a <see cref="IBodyPart"/>.
        ///     For instance, taking a brain out of ones head.
        /// </summary>
        protected virtual void OnRemovedFromPart(IBodyPart old) { }

        /// <summary>
        ///     Called every update when this behavior is connected to a
        ///     <see cref="BodyManagerComponent"/>, but not while in a
        ///     <see cref="DroppedMechanismComponent"/> or
        ///     <see cref="DroppedBodyPartComponent"/>,
        ///     before <see cref="MetabolismComponent.Update"/> is called.
        /// </summary>
        public virtual void PreMetabolism(float frameTime) { }

        /// <summary>
        ///     Called every update when this behavior is connected to a
        ///     <see cref="BodyManagerComponent"/>, but not while in a
        ///     <see cref="DroppedMechanismComponent"/> or
        ///     <see cref="DroppedBodyPartComponent"/>,
        ///     after <see cref="MetabolismComponent.Update"/> is called.
        /// </summary>
        public virtual void PostMetabolism(float frameTime) { }
    }
}
