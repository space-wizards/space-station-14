// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;

namespace Content.Shared.DeadSpace.Demons.Shadowling;

[RegisterComponent, NetworkedComponent]
public sealed partial class ShadowlingRuleComponent : Component
{
    [DataField] public int TargetSlaves = 30;
    [DataField] public int MinTargetSlaves = 20;
    [DataField] public int MaxTargetSlaves = 30;
    [DataField] public int AlertThreshold = 15;
    [DataField] public int MinAlertThreshold = 10;
    [DataField] public int MaxAlertThreshold = 15;
    [ViewVariables] public float Scale;
    [ViewVariables] public bool IsAscended;
    [ViewVariables] public bool AllDead;
    [ViewVariables] public bool ManifestWritten;
    [ViewVariables] public bool HadShadowlings;
    [ViewVariables] public bool AlertAnnounced;
    [ViewVariables] public bool AscendanceAnnounced;
}