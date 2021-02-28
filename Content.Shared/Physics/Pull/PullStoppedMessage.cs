#nullable enable
using Robust.Shared.GameObjects;

namespace Content.Shared.Physics.Pull
{
    public class PullStoppedMessage : PullMessage
    {
        public PullStoppedMessage(IPhysBody puller, IPhysBody pulled) : base(puller, pulled)
        {
        }
    }
}
