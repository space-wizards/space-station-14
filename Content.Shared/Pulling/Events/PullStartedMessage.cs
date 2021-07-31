using Robust.Shared.Physics;

namespace Content.Shared.Physics.Pull
{
    public class PullStartedMessage : PullMessage
    {
        public PullStartedMessage(IPhysBody puller, IPhysBody pulled) :
            base(puller, pulled)
        {
        }
    }
}
