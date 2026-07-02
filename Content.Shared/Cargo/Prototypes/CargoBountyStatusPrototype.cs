using Robust.Shared.Prototypes;

namespace Content.Shared.Cargo.Prototypes;

/// <summary>
/// Defineds the status a bounty can be set as in the bounty computer
/// </summary>
[Prototype]
public sealed partial class CargoBountyStatusPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public int Index = default;

}
