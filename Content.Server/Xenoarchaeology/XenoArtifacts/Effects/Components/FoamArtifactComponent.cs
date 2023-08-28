using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;

/// <summary>
/// Generates foam from the artifact when activated
/// </summary>
[RegisterComponent, Access(typeof(FoamArtifactSystem))]
public sealed partial class FoamArtifactComponent : Component
{
    /// <summary>
    /// The list of reagents that will randomly be picked from
    /// to choose the foam reagent
    /// </summary>
    [DataField("reagents", required: true, customTypeSerializer: typeof(PrototypeIdListSerializer<ReagentPrototype>))]
    public List<string> Reagents = new();

    /// <summary>
    /// The foam reagent
    /// </summary>
    [DataField("selectedReagent"), ViewVariables(VVAccess.ReadWrite)]
    public string? SelectedReagent;

    /// <summary>
    /// How long does the foam last?
    /// </summary>
    [DataField("duration"), ViewVariables(VVAccess.ReadWrite)]
    public float Duration = 10;

    /// <summary>
    /// How much reagent is in the foam?
    /// </summary>
    [DataField("reagentAmount"), ViewVariables(VVAccess.ReadWrite)]
    public float ReagentAmount = 200;

    /// <summary>
    /// Minimum radius of foam spawned
    /// </summary>
    [DataField("minFoamAmount"), ViewVariables(VVAccess.ReadWrite)]
    public int MinFoamAmount = 15;

    /// <summary>
    /// Maximum radius of foam spawned
    /// </summary>
    [DataField("maxFoamAmount"), ViewVariables(VVAccess.ReadWrite)]
    public int MaxFoamAmount = 20;
}
