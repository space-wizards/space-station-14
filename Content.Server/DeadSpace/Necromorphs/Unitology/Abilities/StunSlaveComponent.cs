// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

namespace Content.Shared.DeadSpace.Necromorphs.Unitology.Components;

/// <summary>
/// Used for marking regular unitologs as well as storing icon prototypes so you can see fellow unitologs.
/// </summary>
[RegisterComponent]
public sealed partial class StunSlaveComponent : Component
{
    [DataField]
    public float Duration = 300f;

    [DataField]
    public TimeSpan TimeUtil;
}
