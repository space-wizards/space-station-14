using Robust.Shared.Prototypes;

namespace Content.Shared.Body.Prototypes;

/// <summary>
/// This is a prototype for...
/// </summary>
[Prototype()]
public sealed partial class BodyPartTypePrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;
}
