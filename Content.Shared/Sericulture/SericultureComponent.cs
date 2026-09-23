using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Sericulture;

/// <summary>
/// Should be applied to any mob that you want to be able to produce any material with an action and the cost of hunger.
/// TODO: Probably adjust this to utilize organs?
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSericultureSystem)), AutoGenerateComponentState]
public sealed partial class SericultureComponent : Component
{
    /// <summary>
    /// The text that pops up whenever sericulture fails for not having enough hunger.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId PopupText = "sericulture-failure-hunger";

    /// <summary>
    /// What will be produced at the end of the action.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId EntityProduced;

    /// <summary>
    /// The entity needed to actually preform sericulture. This will be granted (and removed) upon the entity's creation.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId Action = "ActionSericulture";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    /// <summary>
    /// How long will it take to make.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ProductionLength = 3f;

    /// <summary>
    /// This will subtract (not add, don't get this mixed up) from the current hunger of the mob doing sericulture.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HungerCost = 5f;

    /// <summary>
    /// If the entity's hunger satiation is below this value, it cannot spin web.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public SatiationValue MinHungerThreshold;
}
