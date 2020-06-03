using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    public interface ITargetedInteractEventArgs
    {
        /// <summary>
        /// Performer of the attack
        /// </summary>
        IEntity User { get; }
        /// <summary>
        /// Target of the attack
        /// </summary>
        IEntity Target { get; }

    }
}
