using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Species.Arachnid;

/// <summary>
/// Allows an entity to wrap other entities in cocoons. This component adds a wrap action
/// and manages the wrapping behavior including duration, hunger cost, and range.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedCocoonSystem))]
public sealed partial class CocoonerComponent : Component
{
    /// <summary>
    /// The entity prototype ID for the wrap action that gets added to the entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string WrapAction = "ActionArachnidWrap";

    /// <summary>
    /// The entity UID of the wrap action entity.
    /// </summary>
    [DataField("actionEntity"), AutoNetworkedField]
    public EntityUid? ActionEntity;

    /// <summary>
    /// The duration in seconds it takes to wrap a target entity in a cocoon.
    /// </summary>
    [DataField("wrapDuration"), AutoNetworkedField]
    public float WrapDuration = 10f;

    /// <summary>
    /// The reduced duration in seconds for wrapping targets that are stunned, asleep, critical, or dead.
    /// </summary>
    [DataField("wrapDuration_Short"), AutoNetworkedField]
    public float WrapDuration_Short = 3f;

    /// <summary>
    /// The hunger cost required to perform the wrap action. The entity must have at least
    /// this much hunger to successfully wrap a target.
    /// </summary>
    [DataField("hungerCost"), AutoNetworkedField]
    public float HungerCost = 10f;

    /// <summary>
    /// The maximum distance/range threshold for wrapping a target entity.
    /// </summary>
    [DataField("wrapRange"), AutoNetworkedField]
    public float WrapRange = 0.5f;
}
