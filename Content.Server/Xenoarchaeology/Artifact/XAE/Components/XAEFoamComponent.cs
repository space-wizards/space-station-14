using Content.Shared.Chemistry.Reagent;
using Content.Shared.Destructible.Thresholds;
using Robust.Shared.Prototypes;

namespace Content.Server.Xenoarchaeology.Artifact.XAE.Components;

/// <summary>
/// Generates foam from the artifact when activated.
/// </summary>
[RegisterComponent, Access(typeof(XAEFoamSystem))]
public sealed partial class XAEFoamComponent : Component
{
    /// <summary>
    /// The list of reagents that will randomly be picked from
    /// to choose the foam reagent.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<ReagentPrototype>> Reagents = new();

    /// <summary>
    /// The foam reagent.
    /// </summary>
    [DataField]
    public string? SelectedReagent;

    /// <summary>
    /// How long does the foam last?
    /// </summary>
    [DataField]
    public float DefaultDuration = 10f;

    /// <summary>
    /// Min and max value for foam duration.
    /// </summary>
    [DataField]
    public MinMax DurationRestrictions = new MinMax(5, 30);

    /// <summary>
    /// Range for foam spreading.
    /// </summary>
    [DataField]
    public float DefaultRange = 3;

    /// <summary>
    /// Min and max value for foam spreading range.
    /// </summary>
    [DataField]
    public MinMax RangeRestrictions = new MinMax(2, 15);

    /// <summary>
    /// Default amount of foam reagent in foam mass.
    /// </summary>
    [DataField]
    public float DefaultFoamAmount = 20f;

    /// <summary>
    /// Min and max amount of foam reagent in foam mass.
    /// </summary>
    [DataField]
    public MinMax FoamAmountRestrictions = new MinMax(15f, 50f);

    /// <summary>
    /// Marker, if entity where this component is placed should have description replaced with selected chemicals
    /// on component init.
    /// </summary>
    [DataField]
    public bool ReplaceDescription;
}

