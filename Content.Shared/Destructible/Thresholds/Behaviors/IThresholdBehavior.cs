namespace Content.Shared.Destructible.Thresholds.Behaviors
{
    public interface IThresholdBehavior
    {
        /// <summary>
        ///     Executes this behavior.
        /// </summary>
        /// <param name="owner">The entity that owns this behavior.</param>
        /// <param name="collection"></param>
        /// <param name="entManager"></param>
        /// <param name="cause">The entity that caused this behavior.</param>
        /// <param name="system">
        ///     An instance of <see cref="DestructibleSystem"/> to pull dependencies
        ///     and other systems from.
        /// </param>
        void Execute(EntityUid owner,
            IDependencyCollection collection,
            EntityManager entManager,
            EntityUid? cause = null);
    }
}
