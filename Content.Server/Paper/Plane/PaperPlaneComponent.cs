using Robust.Shared.Utility;
using System.Threading;

namespace Content.Server.Paper.Plane
{
    [RegisterComponent]
    public sealed class PaperPlaneComponent : Component
    {
        [DataField("foldDelay")]
        public float FoldDelay = 1.0f;

        [DataField("tags")]
        public string[] Tags = { "NoSpinOnThrow", "Trash" };

        public CancellationTokenSource? CancelToken = null;
    }
}
