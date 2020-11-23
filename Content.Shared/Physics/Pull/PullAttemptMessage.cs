using Robust.Shared.GameObjects.Components;

namespace Content.Shared.Physics.Pull
{
    public class PullAttemptMessage : PullMessage
    {
        public PullAttemptMessage(IPhysicsComponent puller, IPhysicsComponent pulled) : base(puller, pulled) { }

        public bool Cancelled { get; set; }
    }
}
