// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

namespace Content.Server.DeadSpace.Necromorphs.NecroWall.Components;

[RegisterComponent]
public sealed partial class InfestedDeadWallComponent : Component
{
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? NecroWallEntity = null;
}
