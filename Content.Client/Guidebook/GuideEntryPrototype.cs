using Robust.Shared.Prototypes;

namespace Content.Client.Guidebook;

/// <summary>
/// This is a prototype for...
/// </summary>
[Prototype("guideEntry")]
public sealed class GuideEntryPrototype : GuideEntry, IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    public override string Id => ID; // Ew.
}
