using Robust.Shared.Prototypes;

namespace Content.Shared.Cargo.Prototypes;

/// <summary>
/// Defineds the status a bounty can be set as in the bounty computer
/// </summary>
[Prototype]
public sealed partial class CargoBountyStatusPrototype : IPrototype
{
    /// <inheritdoc/>
    /// Default Values
    /// If you change the YAML this must change as well
    /// These will be the status a bounty starts on
    [IdDataField]
    public string ID { get; private set; } = "Undelivered";

    [DataField]
    public int Index = 0;
}
