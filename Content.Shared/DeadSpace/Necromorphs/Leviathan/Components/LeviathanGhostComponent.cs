// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Shared.DeadSpace.Necromorphs.Leviathan.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class LeviathanGhostComponent : Component
{
    [DataField]
    public EntProtoId ReturnToBodyAction = "ActionReturnToBody";

    [DataField]
    public EntityUid? ReturnToBodyActionEntity;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public float RangeTerritory = 1f;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextTick = TimeSpan.Zero;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? MasterEntity = null;
}
