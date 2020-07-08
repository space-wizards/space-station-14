using System;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;

namespace Content.Server.Interfaces.GameObjects.Components.Interaction
{
    /// <summary>
    ///     This interface gives components behavior when being used to "attack".
    /// </summary>
    public interface IAttack
    {
        void Attack(AttackEventArgs eventArgs);
    }

    public class AttackEventArgs : EventArgs
    {
        public AttackEventArgs(IEntity user, GridCoordinates clickLocation)
        {
            User = user;
            ClickLocation = clickLocation;
        }

        public IEntity User { get; }
        public GridCoordinates ClickLocation { get; }
    }
}
