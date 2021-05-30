#nullable enable
using System;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Shared.Interfaces.GameObjects.Components
{
    /// <summary>
    ///     This interface gives components behavior when being used to "attack".
    /// </summary>
    [RequiresExplicitImplementation]
    public interface IAttack
    {
        // Redirects to ClickAttack by default.
        [Obsolete("WideAttack")]
        bool WideAttack(AttackEvent eventArgs) => ClickAttack(eventArgs);

        [Obsolete("Use ClickAttack instead")]
        bool ClickAttack(AttackEvent eventArgs);
    }

    /// <summary>
    ///     Raised when a target entity is attacked by a user.
    /// </summary>
    public class AttackEvent : EntityEventArgs
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
        ///     Indicates whether the attack creates a swing attack or attacks the target entity directly.
        /// </summary>
        public bool WideAttack { get; }

        /// <summary>
        ///     UID of the entity that was attacked.
        /// </summary>
        public EntityUid Target { get; }

        /// <summary>
        ///     Entity that was attacked.
        /// </summary>
        public IEntity? TargetEntity { get; }

        public AttackEvent(IEntity user, EntityCoordinates clickLocation, bool wideAttack, EntityUid target = default)
        {
            User = user;
            ClickLocation = clickLocation;
            WideAttack = wideAttack;
            Target = target;

            IoCManager.Resolve<IEntityManager>().TryGetEntity(Target, out var targetEntity);
            TargetEntity = targetEntity;
        }
    }
}
