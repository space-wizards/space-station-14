using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;

namespace Content.Server.GameObjects.Components.Destructible.Thresholds.Behavior
{
    public interface IThresholdBehavior : IExposeData
    {
        /// <summary>
        ///     Triggers this behavior.
        /// </summary>
        /// <param name="owner">The entity that owns this behavior.</param>
        /// <param name="system">
        ///     An instance of <see cref="DestructibleSystem"/> to pull dependencies
        ///     and other systems from.
        /// </param>
        void Trigger(IEntity owner, DestructibleSystem system);
    }
}
