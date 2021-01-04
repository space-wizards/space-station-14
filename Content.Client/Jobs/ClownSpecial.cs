using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.Serialization;

namespace Content.Client.Jobs
{
    [UsedImplicitly]
    public sealed class ClownSpecial : JobSpecial
    {
        // Dummy class that exists solely to avoid an exception on the client,
        // but allow the server-side counterpart to exist.
        public override IDeepClone DeepClone()
        {
            return new ClownSpecial();
        }
    }
}
