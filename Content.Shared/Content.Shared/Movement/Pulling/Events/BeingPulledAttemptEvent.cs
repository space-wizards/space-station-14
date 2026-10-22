namespace Content.Shared.Pulling.Events
{
    /// <summary>
    ///     Directed event raised on the pulled to see if it can be pulled.
    /// </summary>
    public sealed class BeingPulledAttemptEvent : CancellableEntityEventArgs
    {
        public BeingPulledAttemptEvent(EntityUid puller, EntityUid pulled)
        {
            Puller = puller;
            Pulled = pulled;
        }

        public EntityUid Puller { get; }
        public EntityUid Pulled { get; }
    }
}
