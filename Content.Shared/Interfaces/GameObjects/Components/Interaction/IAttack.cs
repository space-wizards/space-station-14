#nullable enable
using System;
using Content.Shared.GameObjects.Components.Research;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Shared.Interfaces.GameObjects.Components
{
    /// <summary>
    ///     This interface gives components behavior when being used to "attack".
    /// </summary>
    public interface IAttack
    {
        // Redirects to ClickAttack by default.
        bool WideAttack(AttackEventArgs eventArgs) => ClickAttack(eventArgs);
        bool ClickAttack(AttackEventArgs eventArgs);
    }

    public class AttackEventArgs : EventArgs
    {
        public AttackEventArgs(IEntity user, GridCoordinates clickLocation, bool wideAttack, EntityUid target = default)
        {
            User = user;
            ClickLocation = clickLocation;
            WideAttack = wideAttack;
            Target = target;
        }

        public IEntity User { get; }
        public GridCoordinates ClickLocation { get; }
        public bool WideAttack { get; }
        public EntityUid Target { get; }
        public IEntity? TargetEntity => IoCManager.Resolve<IEntityManager>()?.GetEntity(Target) ?? null;
    }
}
