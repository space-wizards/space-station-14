using Content.Shared.Labels.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Labels.Components;

/// <summary>
/// Specifies the type and color of the paper to show on entities this label is attached to.
/// Paper sprites are expected to be greyscaled and recolorable!
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(LabelSystem))]
public sealed partial class PaperLabelTypeComponent : Component
{
    /// <summary>
    /// The type of label to display.
    /// </summary>
    [DataField]
    public PaperLabelType LabelType = PaperLabelType.Paper;

    /// <summary>
    /// The color of this paper label.
    /// </summary>
    [DataField]
    public Color Color = Color.White;
}
