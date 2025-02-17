// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

namespace Content.Server.DeadSpace.Necromorphs.Hive;

[RegisterComponent]
public sealed partial class HiveComponent : Component
{
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Duration = TimeSpan.FromSeconds(60);

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextTick = TimeSpan.FromSeconds(0);
}
