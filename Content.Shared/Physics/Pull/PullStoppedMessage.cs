using Robust.Shared.GameObjects.Components;

namespace Content.Shared.Physics.Pull
{
    public class PullStoppedMessage : PullMessage
    {
        public PullStoppedMessage(PullController controller, IPhysicsComponent puller, IPhysicsComponent pulled) :
            base(controller, puller, pulled)
        {
        }
    }
}
