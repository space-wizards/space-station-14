using Content.Shared.Examine;
using Robust.Shared.Serialization;

namespace Content.Shared.Labels;

/// <summary>
/// Key representing which <see cref="PlayerBoundUserInterface"/> is currently open.
/// Useful when there are multiple UI for an object. Here it's future-proofing only.
/// </summary>
[Serializable, NetSerializable]
public enum HandLabelerUiKey
{
    Key,
}

/// <summary>
/// AppearanceData keys and sprite layers for showing paper labels on entities.
/// </summary>
[Serializable, NetSerializable]
public enum PaperLabelVisuals : byte
{
    /// <summary> The sprite map key to use for displaying the label. </summary>
    Layer,
    /// <summary> The AppearanceData key storing the type of the label. Stores a PaperLabelType enum. </summary>
    LabelType,
    /// <summary> The AppearanceData key storing the color of the label. Stores a Color. </summary>
    LabelColor,
}

/// <summary>
/// The type of paper label a given sprite is.
/// </summary>
[Serializable, NetSerializable]
public enum PaperLabelType : byte
{
    /// <summary>Nothing is attached to this entity.</summary>
    None,
    /// <summary>A business card is attached to this entity.</summary>
    BusinessCard,
    /// <summary>A piece of paper is attached to this entity.</summary>
    Paper,
    /// <summary>A photograph is attached to this entity.</summary>
    Photograph,
    /// <summary>A printout, like a receipt or forensics data is attached to this entity.</summary>
    Printout,
}

/// <summary>
/// An event raised when a hand labeller changes an entity's label.
/// </summary>
[Serializable, NetSerializable]
public sealed class HandLabelerLabelChangedMessage(string label) : BoundUserInterfaceMessage
{
    /// <summary>
    /// The new label on the entity.
    /// </summary>
    public string Label { get; } = label;
}

/// <summary>
/// An event raised when someone is examining a labelled entity.
/// Handlers should write their messages to ExaminedEvent and set Handled.
/// </summary>
/// <param name="Examined">The ExaminedEvent being wrapped.</summary>
[ByRefEvent]
public partial record struct LabelExaminedEvent(ExaminedEvent Examined)
{
    /// <summary>
    /// Whether or not the event was handled.
    /// </summary>
    public bool Handled = false;
}
