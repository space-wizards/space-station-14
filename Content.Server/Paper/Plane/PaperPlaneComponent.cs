using Robust.Shared.Utility;
using System.Threading;

namespace Content.Server.Paper.Plane
{
    [RegisterComponent]
    public sealed class PaperPlaneComponent : Component
    {
        public float FoldDelay = 1.0f;

        public float FrictionRatio = 0.01f;

        public string[] Tags = { "NoSpinOnThrow", "Trash" };

        public CancellationTokenSource? CancelToken = null;
    }
}
