using Robust.Shared.GameObjects.Components;

namespace Content.Shared.Physics.Pull
{
    public class PullStoppedMessage : PullMessage
    {
        public PullStoppedMessage(PullController controller, ICollidableComponent puller, ICollidableComponent pulled) :
            base(controller, puller, pulled)
        {
        }
    }
}
