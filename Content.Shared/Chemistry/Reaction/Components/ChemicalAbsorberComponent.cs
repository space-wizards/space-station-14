using Content.Shared.Chemistry.Reaction.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Chemistry.Reaction.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ChemicalAbsorberComponent : Component
{

    [DataField(customTypeSerializer:typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan UpdateRate = new(0,0,0,1);

    [DataField]
    public TimeSpan LastUpdate;

    [DataField(required: true), AutoNetworkedField]
    public List<string> LinkedSolutions = new();

    /// <summary>
    /// The entity that contains the solution we want to transfer our absorbed reagents to.
    /// If this is null then the reagents are simply deleted.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? TransferTargetEntity = null;

    /// <summary>
    /// SolutionId for our target solution
    /// </summary>
    [DataField, AutoNetworkedField]
    public string TransferTargetSolutionId = "bloodReagents";


    /// <summary>
    /// List of absorption groups, these will be split into single/multi-reagent absorptions and then sorted by priortiy
    /// with multi-reagent absorptions being checked FIRST.
    /// And their reaction rate multipliers
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<AbsorptionGroupPrototype>, float> AbsorptionGroups = new();

    /// <summary>
    /// A list of individual absorptions to add in addition to the ones contained in the absorption groups
    /// And their reaction rate multipliers
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<AbsorptionPrototype>, float>? AdditionalAbsorptions = null;

    /// <summary>
    /// Multiplier for the absorption rate of the chosen absorption (if any)
    /// </summary>
    [DataField, AutoNetworkedField]
    public float GlobalRateMultiplier = 1.0f;

    /// <summary>
    /// List of all the reagent absorption reactions in the order they should be checked
    /// </summary>
    [DataField]
    public List<AbsorptionReaction> CachedAbsorptionOrder = new();
}
