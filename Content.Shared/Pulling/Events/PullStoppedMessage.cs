using Robust.Shared.Physics.Components;

namespace Content.Shared.Physics.Pull
{
    public sealed class PullStoppedMessage : PullMessage
    {
        public PullStoppedMessage(PhysicsComponent puller, PhysicsComponent pulled) : base(puller, pulled)
        {
        }
    }
}
