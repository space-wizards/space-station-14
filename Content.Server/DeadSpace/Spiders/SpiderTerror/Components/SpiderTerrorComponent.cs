// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

namespace Content.Server.DeadSpace.Spiders.SpiderTerror.Components;

[RegisterComponent, Access(typeof(SpiderTerrorSystem))]
public sealed partial class SpiderTerrorComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public string Proto = "CaptureSpiderTerrorObjective";
}
