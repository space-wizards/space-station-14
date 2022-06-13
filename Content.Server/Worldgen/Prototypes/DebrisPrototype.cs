using Robust.Shared.Prototypes;

namespace Content.Server.Worldgen.Prototypes;

/// <summary>
/// This is a prototype for...
/// </summary>
[Prototype("debris")]
public sealed class DebrisPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;
}
