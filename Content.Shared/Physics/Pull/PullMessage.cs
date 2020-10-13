using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;

namespace Content.Shared.Physics.Pull
{
    public class PullMessage : ComponentMessage
    {
        public readonly PullController Controller;
        public readonly IPhysicsComponent Puller;
        public readonly IPhysicsComponent Pulled;

        protected PullMessage(PullController controller, IPhysicsComponent puller, IPhysicsComponent pulled)
        {
            Controller = controller;
            Puller = puller;
            Pulled = pulled;
        }
    }
}
