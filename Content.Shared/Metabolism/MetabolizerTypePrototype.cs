using Robust.Shared.Prototypes;

namespace Content.Shared.Metabolism;

/// <summary>
/// Metabolizer identifier used to determine if a specific entity can metabolize a specific reagent effect.
/// </summary>
[Prototype]
public sealed partial class MetabolizerTypePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("name", required: true)]
    private LocId Name { get; set; }

    [ViewVariables(VVAccess.ReadOnly)]
    public string LocalizedName => Loc.GetString(Name);
}
