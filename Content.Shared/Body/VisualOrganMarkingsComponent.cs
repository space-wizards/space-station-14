using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Body;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true, fieldDeltas: true)]
[Access(typeof(SharedVisualBodySystem))]
public sealed partial class VisualOrganMarkingsComponent : Component
{
    /// <summary>
    /// What markings this organ can take
    /// </summary>
    [DataField(required: true), AlwaysPushInheritance]
    public OrganMarkingData MarkingData = default!;

    /// <summary>
    /// The list of markings to apply to the entity
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<HumanoidVisualLayers, List<Marking>> Markings = new();

    /// <summary>
    /// Layers that are eligible for hiding based on e.g. clothing
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<Enum> HideableLayers = new();

    /// <summary>
    /// A dictionary of layers to other layers that visually depend on them for hiding, e.g. SnoutCover depends on Snout
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<Enum, HashSet<Enum>> DependentHidingLayers = new();

    /// <summary>
    /// Client only - the last markings applied by this component
    /// </summary>
    [ViewVariables]
    public List<Marking> AppliedMarkings = new();
}

/// <summary>
/// Defines the layers and group an organ takes markings for
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
public partial record struct OrganMarkingData
{
    [DataField(required: true)]
    public HashSet<HumanoidVisualLayers> Layers = default!;

    /// <summary>
    /// The type of organ this is for markings
    /// </summary>
    [DataField(required: true)]
    public ProtoId<MarkingsGroupPrototype> Group = default!;
}
