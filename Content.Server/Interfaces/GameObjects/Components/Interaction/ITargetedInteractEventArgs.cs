using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.Interfaces.GameObjects.Components.Interaction
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
