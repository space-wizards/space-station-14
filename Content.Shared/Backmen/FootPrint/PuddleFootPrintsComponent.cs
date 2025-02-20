// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT
// Official port from the BACKMEN project. Make sure to review the original repository to avoid license violations.

namespace Content.Shared.Backmen.FootPrint;

[RegisterComponent]
public sealed partial class PuddleFootPrintsComponent : Component
{
    public float SizeRatio = 0.2f;
    public float OffPercent = 80f;
}
