using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.Blood.Prototypes;

/// <summary>
/// This is a prototype for blood antigens, this effectively acts as an enum for bloodtypes to use.
/// </summary>
[Prototype()]
public sealed partial class BloodAntigenPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;
}
