using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;

namespace Content.Shared.Physics.Pull
{
    public class PullMessage : ComponentMessage
    {
        public readonly IPhysicsComponent Puller;
        public readonly IPhysicsComponent Pulled;

        protected PullMessage(IPhysicsComponent puller, IPhysicsComponent pulled)
        {
            Puller = puller;
            Pulled = pulled;
        }
    }
}
