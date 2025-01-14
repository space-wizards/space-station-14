using Content.Server.Anomaly.Effects;
using Robust.Shared.Prototypes;
namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;


/// <summary>
/// Directly inject chemicals into entities within a range. Do not use for truly dangerous chems, there is no counterplay.
/// </summary>
[RegisterComponent]
public sealed partial class InjectionArtifactComponent : Component
{
    [DataDefinition]
    public sealed partial class ChemEntry
    {
        [DataField("chemical"), ViewVariables(VVAccess.ReadWrite)]
        public string Chemical = "Water";

        [DataField("amount"), ViewVariables(VVAccess.ReadWrite)]
        public float Amount = 1f;
    }

    /// <summary>
    /// Chemicals to inject
    /// </summary>
    [DataField("entries"), ViewVariables(VVAccess.ReadWrite)]
    public ChemEntry[] Entries { get; private set; } = Array.Empty<ChemEntry>();

    /// <summary>
    /// The solution where all the chemicals are stored
    /// </summary>
    [DataField("chemicalSolution", required: true), ViewVariables(VVAccess.ReadWrite)]
    public Solution ChemicalSolution = default!;


    /// <summary>
    /// Distance from the artifact where things can be injected
    /// </summary>
    [DataField("range"), ViewVariables(VVAccess.ReadWrite)]
    public float Range = 5f;


    /// <summary>
    /// The name of the prototype of the special effect that appears above the entities into which the injection was carried out
    /// </summary>
    [DataField("visualEffectPrototype"), ViewVariables(VVAccess.ReadOnly)]
    public EntProtoId VisualEffectPrototype = "PuddleSparkle";

    /// <summary>
    /// Allow the special effect to appear
    /// </summary>
    [DataField("showEffect"), ViewVariables(VVAccess.ReadOnly)]
    public bool ShowEffect = true;
}
