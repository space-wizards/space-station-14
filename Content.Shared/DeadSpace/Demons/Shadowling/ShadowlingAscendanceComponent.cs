// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeadSpace.Demons.Shadowling;

[RegisterComponent, NetworkedComponent]
public sealed partial class ShadowlingAscendanceComponent : Component
{
    [DataField] public EntProtoId ActionAscendance = "ActionShadowlingAscendance";
    [DataField] public EntityUid? ActionAscendanceEntity;

    [DataField] public int RequiredSlaves = 30;
    [DataField] public int MinRequiredSlaves = 20;
    [DataField] public int MaxRequiredSlaves = 30;
    [DataField] public float Duration = 6.74f;
}

public sealed partial class ShadowlingAscendanceEvent : InstantActionEvent { }