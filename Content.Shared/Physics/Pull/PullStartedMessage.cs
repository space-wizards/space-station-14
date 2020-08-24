using Robust.Shared.GameObjects.Components;

namespace Content.Shared.Physics.Pull
{
    public class PullStartedMessage : PullMessage
    {
        public PullStartedMessage(PullController controller, ICollidableComponent puller, ICollidableComponent pulled) :
            base(controller, puller, pulled)
        {
        }
    }
}
