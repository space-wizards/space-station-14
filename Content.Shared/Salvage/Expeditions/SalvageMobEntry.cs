using Content.Shared.Random;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Salvage.Expeditions;

[DataDefinition]
public partial record struct SalvageMobEntry() : IBudgetEntry
{
    /// <summary>
    /// Cost for this mob in a budget.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("cost")]
    public float Cost { get; set; } = 1f;

    /// <summary>
    /// Probability to spawn this mob. Summed with everything else for the faction.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("prob")]
    public float Prob { get; set; } = 1f;

    [ViewVariables(VVAccess.ReadWrite), DataField("proto", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Proto { get; set; } = string.Empty;
}
