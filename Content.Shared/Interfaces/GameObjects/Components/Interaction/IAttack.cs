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
        bool WideAttack(AttackEventArgs eventArgs) => ClickAttack(eventArgs);

        [Obsolete("Use ClickAttack instead")]
        bool ClickAttack(AttackEventArgs eventArgs);
    }

    public class AttackEventArgs : EntityEventArgs
    {
        public AttackEventArgs(IEntity user, EntityCoordinates clickLocation, bool wideAttack, EntityUid target = default)
        {
            User = user;
            ClickLocation = clickLocation;
            WideAttack = wideAttack;
            Target = target;

            IoCManager.Resolve<IEntityManager>().TryGetEntity(Target, out var targetEntity);
            TargetEntity = targetEntity;
        }

        public IEntity User { get; }
        public EntityCoordinates ClickLocation { get; }
        public bool WideAttack { get; }
        public EntityUid Target { get; }
        public IEntity? TargetEntity { get; }
    }
}
