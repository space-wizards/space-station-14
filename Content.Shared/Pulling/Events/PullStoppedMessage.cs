using Content.Shared.Physics.Pull;
using Robust.Shared.Physics.Components;

namespace Content.Shared.Pulling.Events
{
    public sealed class PullStoppedMessage : PullMessage
    {
        public PullStoppedMessage(PhysicsComponent puller, PhysicsComponent pulled) : base(puller, pulled)
        {
        }
    }
}
