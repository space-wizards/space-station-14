using Content.Shared.Labels;
using Content.Shared.Labels.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Client.Labels.Components;

/// <summary>
/// This component controls the visuals for drawing paper label sprites on an entity.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(LabelSystem))]
public sealed partial class PaperLabelVisualsComponent : Component
{
    /// <summary>
    /// The RSI states to use per label type.
    /// Works off of the value of the <see cref="PaperLabelVisuals.LabelType" /> AppearanceData
    /// Note: None does not need to be defined, it will never be read from this dictionary.
    /// </summary>
    [DataField]
    public Dictionary<PaperLabelType, string> LabelStates = new()
    {
        {PaperLabelType.BusinessCard, "business-card"},
        {PaperLabelType.Paper, "paper"},
        {PaperLabelType.Photograph, "photograph"},
        {PaperLabelType.Printout, "printout"},
    };

    /// <summary>
    /// The fallback state to use, in case LabelStates doesn't contain the current label type.
    /// </summary>
    [DataField]
    public string? FallbackLabelState = "paper";

    /// <summary>
    /// If true, allows recoloring the label.
    /// True by default.
    /// </summary>
    [DataField]
    public bool Recolor = true;
}
