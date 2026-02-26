using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.Triggers;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AddStatusEffectOnTriggerComponent : BaseXOnTriggerComponent
{
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId EffectProto;

    [DataField(required: true), AutoNetworkedField]
    public TimeSpan Duration;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class UpdateStatusEffectOnTriggerComponent : BaseXOnTriggerComponent
{
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId EffectProto;

    [DataField, AutoNetworkedField]
    public TimeSpan? Duration;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SetStatusEffectOnTriggerComponent : BaseXOnTriggerComponent
{
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId EffectProto;

    [DataField, AutoNetworkedField]
    public TimeSpan? Duration;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RemoveStatusEffectOnTriggerComponent : BaseXOnTriggerComponent
{
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId EffectProto;

    [DataField, AutoNetworkedField]
    public TimeSpan? Duration;
}
