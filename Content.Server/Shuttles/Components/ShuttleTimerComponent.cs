namespace Content.Server.Shuttles.Components
{
    [RegisterComponent]
    public sealed partial class ShuttleTimerComponent : Component
    {
        // these are hashsets because there's a lot of redundant pairing rn
        // [ViewVariables]
        // public HashSet<EntityUid> LocalScreens = new();

        // [ViewVariables]
        // public HashSet<EntityUid> RemoteScreens = new();
        [DataField("sourceTime"), ViewVariables]
        public TimeSpan? SourceTime;

        [ViewVariables]
        public TimeSpan? Duration;

        [DataField("pairWith"), ViewVariables]
        public RemoteShuttleTimerMask PairWith = RemoteShuttleTimerMask.None;
    }
}
