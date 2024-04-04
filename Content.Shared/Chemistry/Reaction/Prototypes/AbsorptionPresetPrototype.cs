using Robust.Shared.Prototypes;

namespace Content.Shared.Chemistry.Reaction.Prototypes;

/// <summary>
/// This is a prototype for...
/// </summary>
[Prototype]
public sealed partial class AbsorptionPresetPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;
}
