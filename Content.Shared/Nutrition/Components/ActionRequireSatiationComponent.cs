using Content.Shared.FixedPoint;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Prototypes;
using Content.Shared.Popups;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Nutrition.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SatiationSystem))]
public sealed partial class ActionRequireSatiationComponent : Component
{
    /// <summary>
    /// The required satiation type for this ability.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<SatiationTypePrototype> Satiation = SatiationSystem.Hunger;

    /// <summary>
    /// The amount of satiation needed for this ability.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 Amount = 10f;

    /// <summary>
    /// Whether the satiation should be spent once the action is handled.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Spend = true;

    /// <summary>
    /// The popup to show when we don't have enough of the specified satiation type.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId? FailReason = "satiation-not-enough-hunger";

    /// <summary>
    /// The type the popup should show as.
    /// </summary>
    [DataField, AutoNetworkedField]
    public PopupType FailReasonType = PopupType.SmallCaution;
}
