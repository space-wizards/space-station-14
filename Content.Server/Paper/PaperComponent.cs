using Content.Shared.Paper;
using System.Threading;

namespace Content.Server.Paper
{
    [RegisterComponent]
    public sealed class PaperComponent : SharedPaperComponent
    {
        public PaperAction Mode;
        [DataField("content")]
        public string Content { get; set; } = "";

        [DataField("contentSize")]
        public int ContentSize { get; set; } = 500;

        [DataField("stampedBy")]
        public List<string> StampedBy { get; set; } = new();
        /// <summary>
        ///     Stamp to be displayed on the paper, state from beauracracy.rsi
        /// </summary>
        [DataField("stampState")]
        public string? StampState { get; set; }

        public CancellationTokenSource? CancelToken = null;

        /// <summary>
        ///     Delay for folding into a paper plane
        /// </summary>
        [ViewVariables]
        [DataField("foldDelay")]
        public float FoldDelay = 1.0f;
    }
}
