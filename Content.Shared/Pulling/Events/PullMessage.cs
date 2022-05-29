using Robust.Shared.Physics;

namespace Content.Shared.Physics.Pull
{
    public abstract class PullMessage : EntityEventArgs
    {
        public readonly IPhysBody Puller;
        public readonly IPhysBody Pulled;

        protected PullMessage(IPhysBody puller, IPhysBody pulled)
        {
            Puller = puller;
            Pulled = pulled;
        }
    }
}
