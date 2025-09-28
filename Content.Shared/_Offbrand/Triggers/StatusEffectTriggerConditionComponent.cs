using Content.Shared.Trigger.Components.Conditions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.Triggers;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StatusEffectTriggerConditionComponent : BaseTriggerConditionComponent
{
    [DataField, AutoNetworkedField]
    public EntProtoId EffectProto;

    [DataField, AutoNetworkedField]
    public bool Invert;

    [DataField, AutoNetworkedField]
    public bool TargetUser;
}
