using Robust.Shared.Prototypes;

namespace Content.Shared.Cargo.Prototypes;

/// <summary>
/// Used to generically group <see cref="CargoBountyPrototype"/> for iteration
/// </summary>
[Prototype()]
public sealed partial class CargoBountyGroupPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;
}
