using Content.Shared.Containers.ItemSlots;
using Content.Shared.Labels.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Labels.Components;

/// <summary>
/// This component allows you to attach and remove a piece of paper to an entity.
/// See LabelStates for the default name of related sprites.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(LabelSystem))]
public sealed partial class PaperLabelComponent : Component
{
    /// <summary>
    /// The slot where the label is stored.
    /// </summary>
    [DataField]
    public ItemSlot LabelSlot = new();

    /// <summary>
    /// The RSI states to use per label type.
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
