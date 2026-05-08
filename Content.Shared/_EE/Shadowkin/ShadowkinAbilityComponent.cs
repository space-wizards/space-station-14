using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Shadowkin;

[RegisterComponent, NetworkedComponent]
public sealed partial class ShadowkinAbilityComponent : Component
{
    [DataField]
    public EntityUid? CurrentAnchor;

    [DataField]
    public EntProtoId AnchorPrototype = "ShadowkinAnchor";

    [DataField]
    public EntProtoId PlaceActionId = "ActionShadowkinPlaceAnchor";

    [DataField]
    public EntProtoId RecallActionId = "ActionShadowkinRecall";

    [DataField]
    public EntProtoId PlaceEffectPrototype = "ShadowkinAnchorPlaceEffect";

    [DataField]
    public EntProtoId RecallDepartEffectPrototype = "ShadowkinAnchorRecallDepartEffect";

    [DataField]
    public EntProtoId RecallArriveEffectPrototype = "ShadowkinAnchorRecallArriveEffect";

    [DataField]
    public float RecallDelay = 0.25f;

    public bool RecallInProgress;

    [DataField]
    public EntityUid? PlaceActionEntity;

    [DataField]
    public EntityUid? RecallActionEntity;
}
