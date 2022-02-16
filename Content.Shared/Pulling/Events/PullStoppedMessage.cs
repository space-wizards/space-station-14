using Robust.Shared.Physics;

namespace Content.Shared.Physics.Pull
{
    public sealed class PullStoppedMessage : PullMessage
    {
        public PullStoppedMessage(IPhysBody puller, IPhysBody pulled) : base(puller, pulled)
        {
        }
    }
}
