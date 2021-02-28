#nullable enable
using Robust.Shared.GameObjects;

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
