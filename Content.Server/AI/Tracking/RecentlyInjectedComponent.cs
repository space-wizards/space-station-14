namespace Content.Server.AI.Tracking
{
    /// Added when a medibot injects someone
    /// So they don't get injected again for at least a minute.
    [RegisterComponent]
    public sealed class RecentlyInjectedComponent : Component
    {
        public TimeSpan RemoveTime = TimeSpan.FromMinutes(1);
    }
}
