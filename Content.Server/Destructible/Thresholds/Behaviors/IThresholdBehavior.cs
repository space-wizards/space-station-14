namespace Content.Server.Destructible.Thresholds.Behaviors
{
    public interface IThresholdBehavior
    {
        /// <summary>
        ///     Executes this behavior.
        /// </summary>
        /// <param name="owner">The entity that owns this behavior.</param>
        /// <param name="system">
        ///     An instance of <see cref="DestructibleSystem"/> to pull dependencies
        ///     and other systems from.
        /// </param>
        void Execute(EntityUid owner, DestructibleSystem system);
    }
}
