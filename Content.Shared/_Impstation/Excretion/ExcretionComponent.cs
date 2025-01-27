using Content.Shared.Nutrition.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Excretion;

/// <summary>
/// Should be applied to any mob that you want to be able to produce any material with an action and the cost of thirst.
/// TODO: Probably adjust this to utilize organs?
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedExcretionSystem)), AutoGenerateComponentState]
public sealed partial class ExcretionComponent : Component
{
    /// <summary>
    /// The text that pops up whenever excretion fails for not having enough thirst.
    /// </summary>
    [DataField("popupText")]
    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public string PopupText = "excretion-failure-thirst";

    /// <summary>
    /// The entity needed to actually preform excretion. This will be granted (and removed) upon the entity's creation.
    /// </summary>
    [DataField(required: true)]
    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public EntProtoId Action;

    [AutoNetworkedField]
    [DataField("actionEntity")]
    public EntityUid? ActionEntity;

    /// <summary>
    /// How long will it take to make.
    /// </summary>
    [DataField("productionLength")]
    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public float ProductionLength = 3f;

    /// <summary>
    /// This will subtract (not add, don't get this mixed up) from the current thirst of the mob doing excretion.
    /// </summary>
    [DataField("thirstCost")]
    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public float ThirstCost = 5f;

    /// <summary>
    /// The lowest thirst threshold that this mob can be in before it's allowed to excrete.
    /// </summary>
    [DataField("minThirstThreshold")]
    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public ThirstThreshold MinThirstThreshold = ThirstThreshold.Okay;

	    /// <summary>
    /// The amount of slowdown applied to snails.
    /// </summary>
    [DataField("snailSlowdownModifier"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public float SnailSlowdownModifier = 0.5f;

	/// The reagent to be spilled.
	[DataField("excretedReagent")]
	[ViewVariables(VVAccess.ReadWrite)]
	[AutoNetworkedField]
	public string ExcretedReagent = "Mucin";

    /// <summary>
    /// The amount of reagent to be spilled.
    /// </summary>
    [DataField("excretedVolume"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public float ExcretedVolume = 15f;

}
