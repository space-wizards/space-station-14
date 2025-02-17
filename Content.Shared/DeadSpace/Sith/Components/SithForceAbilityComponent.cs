// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Prototypes;
using Robust.Shared.Audio;
using Content.Shared.Alert;
using Robust.Shared.GameStates;

namespace Content.Shared.DeadSpace.Sith.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]

public sealed partial class SithForceAbilityComponent : Component
{
    [DataField]
    public EntProtoId ActionSithForce = "ActionSithForce";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionSithForceEntity;

    [DataField]
    public EntProtoId ActionSithForceOnce = "ActionSithForceOne";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionSithForceOneEntity;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float Range = 5f;

    [DataField]
    public float StunDuration = 5f;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float BaseRadialAcceleration = -10f;

    [DataField]
    public float StrenghtPush = 1f;

    [DataField]
    public float StrenghtPull = 0.5f;

    [DataField]
    public float StrenghtPushPullOne = 15f;


    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float BaseTangentialAcceleration = 0f;

    [DataField]
    public SoundSpecifier? SoundPush = default;

    [DataField]
    public SoundSpecifier? SoundPull = default;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public int NumberOfPulses = 3;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsActiveAbility = false;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextPulseTime;

    [ViewVariables(VVAccess.ReadOnly)]
    public float NextPulseDuration = 0.5f;

    [ViewVariables(VVAccess.ReadOnly)]
    public int Pulse = 0;

    [ViewVariables(VVAccess.ReadOnly)]
    public float Strenght;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public EntProtoId PullForcePower = "PullForcePower";

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public EntProtoId PushForcePower = "PushForcePower";

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public ProtoId<AlertPrototype> ForcePowerAlert = "ForcePower";

}

public sealed partial class ChangeForcePowerAlertEvent : BaseAlertEvent;
