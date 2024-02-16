using Content.Shared.Paper;
using Robust.Shared.GameStates;

namespace Content.Server.Paper;

[NetworkedComponent, RegisterComponent]
public sealed partial class PaperComponent : SharedPaperComponent
{
    public PaperAction Mode;
    [DataField("content")]
    public string Content { get; set; } = "";

    [DataField("contentSize")]
    public int ContentSize { get; set; } = 6000;

    [DataField("stampedBy")]
    public List<StampDisplayInfo> StampedBy { get; set; } = new();

    /// <summary>
    ///     Stamp to be displayed on the paper, state from beauracracy.rsi
    /// </summary>
    [DataField("stampState")]
    public string? StampState { get; set; }
}
