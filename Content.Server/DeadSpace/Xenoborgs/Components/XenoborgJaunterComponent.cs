// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

namespace Content.Server.DeadSpace.Xenoborgs.Components;

/// <summary>
/// Returns its user to a safe tile near a xenoborg mothership core.
/// </summary>
[RegisterComponent]
public sealed partial class XenoborgJaunterComponent : Component
{
    [DataField]
    public float SearchRadius = 5f;
}
