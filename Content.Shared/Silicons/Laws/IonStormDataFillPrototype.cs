using Robust.Shared.Prototypes;

namespace Content.Shared.Silicons.Laws;

/// <summary>
/// A prototype for a reusable set of ion storm law targets.
/// </summary>
[Prototype]
public sealed partial class IonStormDataFillPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The list of selectors to pick from.
    /// </summary>
    [DataField]
    public List<IonLawSelector> Targets { get; private set; } = new();
}

/// <summary>
/// Selects a random value from an IonStormDataFill prototype.
/// </summary>
[DataDefinition]
public sealed partial class IonStormDataFill : IonLawSelector
{
    /// <summary>
    /// The IonStormDataFill prototype to use.
    /// </summary>
    [DataField]
    public ProtoId<IonStormDataFillPrototype> Target { get; private set; }
}
