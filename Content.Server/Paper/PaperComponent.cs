using Content.Server.UserInterface;
using Content.Shared.Paper;
using Robust.Server.GameObjects;

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
        [ViewVariables] public BoundUserInterface? UserInterface => Owner.GetUIOrNull(PaperUiKey.Key);
    }
}
