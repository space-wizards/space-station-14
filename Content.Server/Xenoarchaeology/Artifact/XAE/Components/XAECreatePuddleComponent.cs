using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Destructible.Thresholds;
using Robust.Shared.Prototypes;

namespace Content.Server.Xenoarchaeology.Artifact.XAE.Components;

/// <summary>
/// This is used for an artifact that creates a puddle of
/// random chemicals upon being triggered.
/// </summary>
[RegisterComponent, Access(typeof(XAECreatePuddleSystem))]
public sealed partial class XAECreatePuddleComponent : Component
{
    /// <summary>
    /// The solution where all the chemicals are stored.
    /// </summary>
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public Solution ChemicalSolution = default!;

    /// <summary>
    /// The different chemicals that can be spawned by this effect.
    /// </summary>
    [DataField]
    public List<ProtoId<ReagentPrototype>> PossibleChemicals = new();

    /// <summary>
    /// The number of chemicals in the puddle.
    /// </summary>
    [DataField]
    public MinMax ChemAmount = new MinMax(1, 3);

    /// <summary>
    /// List of reagents selected for this node. Selected ones are chosen on first activation
    /// and are picked from <see cref="PossibleChemicals"/> and is calculated separately for each node.
    /// </summary>
    [DataField]
    public List<ProtoId<ReagentPrototype>>? SelectedChemicals;

    /// <summary>
    /// Marker, if entity where this component is placed should have description replaced with selected chemicals
    /// on component init.
    /// </summary>
    [DataField]
    public bool ReplaceDescription;
}
