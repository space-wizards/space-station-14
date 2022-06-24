using Robust.Shared.Containers;
using System.Threading;

namespace Content.Server.Paper.Plane
{
    [RegisterComponent]
    public sealed class PaperPlaneComponent : Component
    {
        // stores the paper inside to retain writing/stamps/etc.
        public ContainerSlot PaperContainer = default!;

        public float FoldDelay = 1.0f;

        public CancellationTokenSource? CancelToken = null;

        protected override void Initialize()
        {
            base.Initialize();

            PaperContainer = ContainerHelpers.EnsureContainer<ContainerSlot>(Owner, $"{Name}-paperContainer");
        }
    }
}
