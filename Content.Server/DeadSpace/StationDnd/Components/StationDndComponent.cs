// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.StationDnd;
using Robust.Shared.Prototypes;

namespace Content.Server.DeadSpace.StationDnd.Components;

[RegisterComponent]
public sealed partial class StationDndComponent : Component
{
    [DataField("configs")]
    [ViewVariables]
    public List<ProtoId<DndPrototype>> Configs = new();
}
