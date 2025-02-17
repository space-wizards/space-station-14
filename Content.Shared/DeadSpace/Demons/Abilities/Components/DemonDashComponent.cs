// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeadSpace.Demons.Abilities.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class DemonDashComponent : Component
{
    [DataField]
    public EntProtoId DemonDashAction = "ActionDemonDash";

    [DataField, AutoNetworkedField]
    public EntityUid? DemonDashActionEntity;

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan AddChargeDuration = TimeSpan.FromSeconds(15);

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan AddChargeTime = TimeSpan.FromSeconds(0);
}
