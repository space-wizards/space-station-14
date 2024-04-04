using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chemistry.Reaction.Prototypes;

[Prototype]
public sealed partial class AbsorptionPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("name")]
    public string Name { get; private set; } = string.Empty;

    [DataField("minTemp")]
    public float MinimumTemperature = 0.0f;

    [DataField("maxTemp")]
    public float MaximumTemperature = float.PositiveInfinity;

    /// <summary>
    /// Should absorbing these reagents transfer their heat to the absorbing entity.
    /// In most cases this should be false because entities that care about heat tend to handle a solution's heat themselves.
    /// If you enable this in that case, it will result in heat being transferred twice. Once by the system the entity's heat
    /// and solutions, and again when the solution is absorbed.
    /// </summary>
    [DataField("transferHeat")]
    public bool TransferHeat;

    /// <summary>
    /// Effects to be triggered when the reagents are absorbed
    /// </summary>
    [DataField("effects")]
    public List<ReagentEffect> Effects = new();

    /// <summary>
    /// How dangerous is this effect? Generally
    /// </summary>
    [DataField("impact", serverOnly: true)]
    public LogImpact Impact = LogImpact.Low;
}
