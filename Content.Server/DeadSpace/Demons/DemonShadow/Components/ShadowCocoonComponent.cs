// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Containers;

namespace Content.Server.DeadSpace.Demons.DemonShadow.Components;

[RegisterComponent]
public sealed partial class ShadowCocoonComponent : Component
{
    public Container Stomach = default!;

    public TimeSpan NextTick;

    [DataField]
    public float Range = 10f;

    public List<EntityUid> PointEntities = new();
}
