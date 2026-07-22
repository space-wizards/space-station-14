// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System.Numerics;

namespace Content.Shared.DeadSpace.Sticky;

/// <summary>
/// Changes an item's sprite scale while it is stuck to a marked surface.
/// </summary>
[RegisterComponent]
public sealed partial class ScaleWhenStuckComponent : Component
{
    [DataField]
    public Vector2 Scale = new(0.7f, 0.7f);

    public Vector2? OriginalScale;
}
