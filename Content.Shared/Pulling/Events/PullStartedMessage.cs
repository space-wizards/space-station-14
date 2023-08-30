using Robust.Shared.Physics.Components;

namespace Content.Shared.Physics.Pull
{
    public sealed class PullStartedMessage : PullMessage
    {
        public PullStartedMessage(PhysicsComponent puller, PhysicsComponent pulled) :
            base(puller, pulled)
        {
        }
    }
}
