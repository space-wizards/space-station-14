using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;

/// <summary>
/// Generates foam from the artifact when activated
/// </summary>
[RegisterComponent]
public sealed class FoamArtifactComponent : Component
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
    [ViewVariables(VVAccess.ReadWrite)]
    public string? SelectedReagent;

    /// <summary>
    /// How long does the foam last?
    /// </summary>
    [DataField("duration")]
    public float Duration = 10;

    /// <summary>
    /// How much reagent is in the foam?
    /// </summary>
    [DataField("reagentAmount")]
    public float ReagentAmount = 100;

    /// <summary>
    /// Minimum radius of foam spawned
    /// </summary>
    [DataField("minFoamAmount")]
    public int MinFoamAmount = 2;

    /// <summary>
    /// Maximum radius of foam spawned
    /// </summary>
    [DataField("maxFoamAmount")]
    public int MaxFoamAmount = 6;

    /// <summary>
    /// How long it takes for each tile of foam to spawn
    /// </summary>
    [DataField("spreadDuration")]
    public float SpreadDuration = 1;
}
