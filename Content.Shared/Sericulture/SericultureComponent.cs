using Content.Shared.Cloning;
using Content.Shared.Nutrition.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Sericulture;

/// <summary>
/// Should be applied to any mob that you want to be able to produce any material with an action and the cost of hunger.
/// TODO: Probably adjust this to utilize organs?
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedSericultureSystem), typeof(CloningContext))]
public sealed partial class SericultureComponent : Component
{
    /// <summary>
    /// The text that pops up whenever sericulture fails for not having enough hunger.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public string PopupText = "sericulture-failure-hunger";

    /// <summary>
    /// What will be produced at the end of the action.
    /// </summary>
    [DataField(required: true)]
    [AutoNetworkedField]
    public EntProtoId EntityProduced;

    /// <summary>
    /// The entity needed to actually preform sericulture. This will be granted (and removed) upon the entity's creation.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public EntProtoId Action = "ActionSericulture";

    [AutoNetworkedField]
    [DataField]
    public EntityUid? ActionEntity;

    /// <summary>
    /// How long will it take to make.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public float ProductionLength = 3f;

    /// <summary>
    /// This will subtract (not add, don't get this mixed up) from the current hunger of the mob doing sericulture.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public float HungerCost = 5f;

    /// <summary>
    /// The lowest hunger threshold that this mob can be in before it's allowed to spin silk.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public HungerThreshold MinHungerThreshold = HungerThreshold.Okay;
}
