// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;

namespace Content.Shared.DeadSpace.Demons.Shadowling;

[RegisterComponent, NetworkedComponent]
public sealed partial class ShadowlingRecruitObjectiveComponent : Component
{
    [DataField]
    public int TargetCount = 30;

    [DataField]
    public int MinTargetCount = 20;

    [DataField]
    public int MaxTargetCount = 30;
}