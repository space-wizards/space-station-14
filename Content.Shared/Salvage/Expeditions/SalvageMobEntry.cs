using Content.Shared.Random;
using Robust.Shared.Prototypes;

namespace Content.Shared.Salvage.Expeditions;

[DataDefinition]
public partial record struct SalvageMobEntry() : IBudgetEntry
{
    /// <summary>
    /// Cost for this mob in a budget.
    /// </summary>
    [DataField]
    public float Cost { get; set; } = 1f;

    /// <summary>
    /// Probability to spawn this mob. Summed with everything else for the faction.
    /// </summary>
    [DataField]
    public float Prob { get; set; } = 1f;

    /// <summary>
    /// The mob to spawn
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Proto { get; set; }
}
