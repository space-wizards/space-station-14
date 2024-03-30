using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.Circulatory.Prototypes;

/// <summary>
/// This is a prototype for defining blood in a circulatory system.
/// </summary>
[Prototype()]
public sealed partial class BloodDefinitionPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// The reagent that represents the combination of both bloodcells and plasma.
    /// This is the reagent used as blood in bloodstream.
    /// </summary>
    [DataField(required:true)]
    public ProtoId<ReagentPrototype> WholeBloodReagent;

    /// <summary>
    /// The reagent used for blood cells in this blood definition, this may hold any number of antibodies.
    /// This is used for blood donations or when filtering.
    /// </summary>
    [DataField(required:true)]
    public ProtoId<ReagentPrototype> BloodCellsReagent;

    /// <summary>
    /// The reagent used for blood plasma in this blood definition, this may hold any number of antibodies.
    /// This is used for plasma donations or when filtering.
    /// </summary>
    [DataField(required:true)]
    public ProtoId<ReagentPrototype> BloodPlasmaReagent;

    /// <summary>
    /// A dictionary containing all the bloodtypes supported by this blood definition and their chance of being
    /// selected when initially creating a bloodstream
    /// </summary>
    [DataField(required:true)]
    public Dictionary<ProtoId<BloodTypePrototype>, FixedPoint2> BloodTypeDistribution = new();

    public ICollection<ProtoId<BloodTypePrototype>> BloodTypes => BloodTypeDistribution.Keys;
}
