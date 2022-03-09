using Content.Shared.Paper;

namespace Content.Client.Paper
{
    [RegisterComponent]
    public sealed class PaperComponent : SharedPaperComponent
    {
        [DataField("hugeUI")]
        public bool HugeUI = false;
    }
}
