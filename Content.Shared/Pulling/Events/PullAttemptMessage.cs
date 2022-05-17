using Robust.Shared.Physics;

namespace Content.Shared.Physics.Pull
{
    public sealed class PullAttemptMessage : PullMessage
    {
        public PullAttemptMessage(IPhysBody puller, IPhysBody pulled) : base(puller, pulled) { }

        public bool Cancelled { get; set; }
    }
}
