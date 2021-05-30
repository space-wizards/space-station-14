#nullable enable
using Robust.Shared.GameObjects;

namespace Content.Shared.Interfaces.GameObjects.Components
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
