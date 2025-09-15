// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Prototypes;

namespace Content.Server.DeadSpace.HardsuitSpeedBuff;

[RegisterComponent]
public sealed partial class HardsuitSpeedBuffComponent : Component
{
    [DataField]
    public EntProtoId Action = "ActionHardsuitSpeedBuff";

    [DataField]
    public EntityUid? ActionEntity;

    [DataField]
    public float WalkModifier = 1.35f;

    [DataField]
    public float SprintModifier = 1.35f;

    [DataField]
    public bool Activated = false;
}
