// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Prototypes;
using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;

namespace Content.Shared.DeadSpace.Sith.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class SithSubordinateComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "SithSubordinateFaction";

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid Master = default;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsSubordinate = true;
}
