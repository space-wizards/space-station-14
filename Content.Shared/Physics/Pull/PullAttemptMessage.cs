#nullable enable
using Robust.Shared.GameObjects;

namespace Content.Shared.Physics.Pull
{
    public class PullAttemptMessage : PullMessage
    {
        public PullAttemptMessage(IPhysBody puller, IPhysBody pulled) : base(puller, pulled) { }

        public bool Cancelled { get; set; }
    }
}
