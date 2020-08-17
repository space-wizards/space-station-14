using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;

namespace Content.Shared.Physics.Pull
{
    public class PullMessage : ComponentMessage
    {
        public readonly PullController Controller;
        public readonly ICollidableComponent Puller;
        public readonly ICollidableComponent Pulled;

        protected PullMessage(PullController controller, ICollidableComponent puller, ICollidableComponent pulled)
        {
            Controller = controller;
            Puller = puller;
            Pulled = pulled;
        }
    }
}
