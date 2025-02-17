// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

namespace Content.Server.DeadSpace.Photocopier;

[RegisterComponent]
public sealed partial class TonerCartridgeComponent : Component
{
    [DataField("maxAmount")]
    public int MaxAmount = 30;

    [DataField("currentAmount")]
    public int CurrentAmount = 30;
}
