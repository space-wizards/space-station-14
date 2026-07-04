using Robust.Shared.Prototypes;

namespace Content.Shared.Cargo.Prototypes;

/// <summary>
/// Defines a "market" that a cargo computer can access and make orders from.
/// </summary>
[Prototype]
public sealed partial class CargoMarketPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;
}
