#nullable enable
using System;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Shared.Interfaces.GameObjects.Components
{
    /// <summary>
    ///     Raised when a target entity is attacked by a user.
    /// </summary>
    public class NormalAttackEvent : EntityEventArgs
    {
        /// <summary>
        ///     Entity that triggered the attack.
        /// </summary>
        public IEntity User { get; }

        /// <summary>
        ///     The original location that was clicked by the user.
        /// </summary>
        public EntityCoordinates ClickLocation { get; }

        /// <summary>
        ///     UID of the entity that was attacked.
        /// </summary>
        public EntityUid Target { get; }

        /// <summary>
        ///     Entity that was attacked.
        /// </summary>
        public IEntity? TargetEntity { get; }

        /// <summary>
        ///     Modified by the handler to indicate whether the attack succeeded.
        /// </summary>
        public bool Succeeded { get; set;  }

        public NormalAttackEvent(IEntity user, EntityCoordinates clickLocation, EntityUid target = default)
        {
            User = user;
            ClickLocation = clickLocation;
            Target = target;

            IoCManager.Resolve<IEntityManager>().TryGetEntity(Target, out var targetEntity);
            TargetEntity = targetEntity;
        }
    }

    /// <summary>
    ///     Raised when a target entity is wide attacked by a user.
    /// </summary>
    public class WideAttackEvent : EntityEventArgs
    {
        /// <summary>
        ///     Entity that triggered the attack.
        /// </summary>
        public IEntity User { get; }

        /// <summary>
        ///     The original location that was clicked by the user.
        /// </summary>
        public EntityCoordinates ClickLocation { get; }

        /// <summary>
        ///     Modified by the handler to indicate whether the attack succeeded.
        /// </summary>
        public bool Succeeded { get; set;  }

        public WideAttackEvent(IEntity user, EntityCoordinates clickLocation)
        {
            User = user;
            ClickLocation = clickLocation;
        }
    }
}
