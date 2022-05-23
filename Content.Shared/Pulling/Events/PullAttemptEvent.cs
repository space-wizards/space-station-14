using Robust.Shared.Physics;

namespace Content.Shared.Physics.Pull
{
    public sealed class PullAttemptEvent : PullMessage
    {
        public PullAttemptEvent(IPhysBody puller, IPhysBody pulled) : base(puller, pulled) { }

        public bool Cancelled { get; set; }
    }
}
