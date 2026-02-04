using Content.Shared.Database;

namespace Content.Server.Destructible.Thresholds.Behaviors
{
    public interface IThresholdBehavior
    {
        public LogImpact Impact => LogImpact.Low;

        /// <summary>
        ///     Executes this behavior.
        /// </summary>
        /// <param name="owner">The entity that owns this behavior.</param>
        /// <param name="system">
        ///     An instance of <see cref="DestructibleSystem"/> to pull dependencies
        ///     and other systems from.
        /// </param>
        /// <param name="cause">The entity that caused this behavior.</param>
        void Execute(EntityUid owner, DestructibleSystem system, EntityUid? cause = null);
    }
}
