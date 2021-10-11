using Robust.Shared.GameObjects;

namespace Content.Shared.Pulling.Events
{
    /// <summary>
    ///     Directed event raised on the puller to see if it can start pulling something.
    /// </summary>
    public class StartPullAttemptEvent : CancellableEntityEventArgs
    {
        public StartPullAttemptEvent(IEntity puller, IEntity pulled)
        {
            Puller = puller;
            Pulled = pulled;
        }

        public IEntity Puller { get; }
        public IEntity Pulled { get; }
    }
}
