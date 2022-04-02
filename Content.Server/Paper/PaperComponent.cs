using Content.Shared.Paper;

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
    }
}
