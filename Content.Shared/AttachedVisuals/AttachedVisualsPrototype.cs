using Robust.Shared.Prototypes;

namespace Content.Shared.AttachedVisuals;

/// <summary>
/// Defines an attachment point ID.
/// Set as a prototype to ensure consistency
/// </summary>
[Prototype]
public sealed partial class VisualAttachmentPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    /// <summary>
    /// Default sprite state. Convenience for things like inhands or clothing.
    /// Ignored if the attachment sets it's own state.
    /// </summary>
    [DataField]
    public string? DefaultState;
}
