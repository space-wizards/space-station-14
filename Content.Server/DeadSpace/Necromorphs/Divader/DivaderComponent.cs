// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Prototypes;

namespace Content.Server.DeadSpace.Divader;

[RegisterComponent]
public sealed partial class DivaderComponent : Component
{
    [DataField("mobDivaderRH")]
    public EntProtoId RHMobSpawnId = "MobDivaderRH";

    [DataField("mobDivaderH")]
    public EntProtoId HMobSpawnId = "MobDivaderH";

    [DataField("mobDivaderLH")]
    public EntProtoId LHMobSpawnId = "MobDivaderLH";
}
