using System;
using System.Collections.Generic;
using Content.Shared.BodySystem;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.BodySystem {

    /// <summary>
    ///     The behaviors a mechanism performs.
    /// </summary>
    public abstract class MechanismBehavior {

        /// <summary>
        ///     Upward reference to the parent <see cref="Mechanism"/> that this behavior is attached to. This reference must exist and cannot be changed after creation.
        /// </summary>
        protected Mechanism ParentMechanism;

        public MechanismBehavior(Mechanism parent)
        {
            ParentMechanism = parent;
            Initialize();
        }

        public abstract void Initialize();

        /// <summary>
        ///     Called when the containing <see cref="BodyPart"/> is attached to a <see cref="BodyManagerComponent"/>. For instance, attaching a head to a body will call this on the brain inside.
        /// </summary>
        public abstract void OnInstallIntoBodyPartManager(IEntity attachedEntity);

        /// <summary>
        ///     Called when the parent <see cref="Mechanism"/> is installed into a <see cref="BodyPart"/>. For instance, putting a brain into an empty head.
        /// </summary>
        public abstract void OnInstallIntoBodyPart(IEntity attachedEntity);

        /// <summary>
        ///     Called when the containing <see cref="BodyPart"/> is removed from a <see cref="BodyManagerComponent"/>. For instance, cutting off one's head will call this on the brain inside.
        /// </summary>
        public abstract void OnRemoveFromBodyPartManager(IEntity attachedEntity);

        /// <summary>
        ///     Called when the parent <see cref="Mechanism"/> is removed from a <see cref="BodyPart"/>. For instance, taking a brain out of one's head.
        /// </summary>
        public abstract void OnRemoveFromBodyPart(IEntity attachedEntity);

        /// <summary>
        ///     Called every tick when this behavior is connected to a <see cref="BodyManagerComponent"/>, but not while in a <see cref="DroppedMechanismComponent"/> or <see cref="DroppedBodyPartComponent"/>.
        /// </summary>
        public abstract void Tick(float frameTime);







    }
}

