using Robust.Shared.Physics.Components;

namespace Content.Shared.Movement.Pulling.Events
{
    public sealed class PullStoppedMessage : PullMessage
    {
        public PullStoppedMessage(PhysicsComponent puller, PhysicsComponent pulled) : base(puller, pulled)
        {
        }
    }
}
