#nullable enable
using Robust.Shared.GameObjects;

namespace Content.Shared.Physics.Pull
{
    public class PullStoppedMessage : PullMessage
    {
        public PullStoppedMessage(IPhysicsComponent puller, IPhysicsComponent pulled) : base(puller, pulled)
        {
        }
    }
}
