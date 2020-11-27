using Robust.Shared.GameObjects.Components;

namespace Content.Shared.Physics.Pull
{
    public class PullStartedMessage : PullMessage
    {
        public PullStartedMessage(IPhysicsComponent puller, IPhysicsComponent pulled) :
            base(puller, pulled)
        {
        }
    }
}
