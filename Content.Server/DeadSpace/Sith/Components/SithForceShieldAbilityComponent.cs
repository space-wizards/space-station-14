// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Hands.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.DeadSpace.Sith.Components;

[RegisterComponent]
public sealed partial class SithForceShieldAbilityComponent : Component
{
    [DataField("proto"), ViewVariables(VVAccess.ReadOnly)]
    public string ShieldPrototypeId = "SithForceShield";

    public EntityUid ShieldPrototype;

    [DataField]
    public EntProtoId ActionSithShield = "ActionSithShield";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionSithShieldEntity;

    [DataField]
    public float Duration = 10f;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan TimeUtil;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsActiveAbility = false;

    [ViewVariables(VVAccess.ReadOnly)]
    public Hand HandShield;
}
