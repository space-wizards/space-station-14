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
}
