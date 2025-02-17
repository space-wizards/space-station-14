// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Shared.DeadSpace.Events.Roles.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class EventRoleComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<FactionIconPrototype> FactionIcon { get; set; } = "EventFaction";
}
