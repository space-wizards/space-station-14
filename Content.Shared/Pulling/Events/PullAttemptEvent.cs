using Robust.Shared.Physics.Components;

namespace Content.Shared.Physics.Pull
{
    public sealed class PullAttemptEvent : PullMessage
    {
        public PullAttemptEvent(PhysicsComponent puller, PhysicsComponent pulled) : base(puller, pulled) { }

        public bool Cancelled { get; set; }
    }
}
