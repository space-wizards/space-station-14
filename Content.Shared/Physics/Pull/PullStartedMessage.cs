#nullable enable
using Robust.Shared.GameObjects;

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
