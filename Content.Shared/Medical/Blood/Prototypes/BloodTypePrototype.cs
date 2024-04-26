using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.Blood.Prototypes;

/// <summary>
/// This is a prototype for defining blood groups (O, A, B, AB, etc.)
/// </summary>
[Prototype]
public sealed partial class BloodTypePrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// What percentage of the blood is plasma, the rest are bloodCells
    /// </summary>
    [DataField]
    public float PlasmaPercentage = 0.55f;

    /// <summary>
    /// Which antigens are present in this blood type's blood cells
    /// </summary>
    [DataField]
    public List<ProtoId<BloodAntigenPrototype>> BloodCellAntigens = new();

    /// <summary>
    /// Which antigens are present in this blood type's blood plasma
    /// </summary>
    [DataField]
    public List<ProtoId<BloodAntigenPrototype>> PlasmaAntigens = new();

    /// <summary>
    /// The reagent that represents the combination of both bloodcells and plasma.
    /// This is the reagent used as blood in bloodstream.
    /// </summary>
    [DataField]
    public ProtoId<ReagentPrototype> WholeBloodReagent = "Blood";

    /// <summary>
    /// The reagent used for blood cells in this blood definition, this may hold any number of antibodies.
    /// This is used for blood donations or when filtering.
    /// </summary>
    [DataField]
    public ProtoId<ReagentPrototype> BloodCellsReagent;

    /// <summary>
    /// The reagent used for blood plasma in this blood definition, this may hold any number of antibodies.
    /// This is used for plasma donations or when filtering.
    /// </summary>
    [DataField]
    public ProtoId<ReagentPrototype> BloodPlasmaReagent;
}
