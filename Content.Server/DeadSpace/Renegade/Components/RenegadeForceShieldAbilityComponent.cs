// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Prototypes;

namespace Content.Server.DeadSpace.Renegade.Components;

[RegisterComponent]
public sealed partial class RenegadeForceShieldAbilityComponent : Component
{
    [DataField("proto"), ViewVariables(VVAccess.ReadOnly)]
    public string ShieldPrototypeId = "RenegadeForceShield";

    public EntityUid ShieldPrototype;

    [DataField]
    public EntProtoId ActionRenegadeShield = "ActionRenegadeShield";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionRenegadeShieldEntity;

    [DataField]
    public float Duration = 10f;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan TimeUtil;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsActiveAbility = false;

    [ViewVariables(VVAccess.ReadOnly)]
    public string? HandShieldId;
}
