// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeadSpace.Demons.Herald.Components;

[RegisterComponent, NetworkedComponent, EntityCategory("Spawner")]
public sealed partial class DemonPortalComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan AnnounceDuration = TimeSpan.FromSeconds(300);

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan AnnounceTime = TimeSpan.FromSeconds(0);

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan DemonSpawnDuration = TimeSpan.FromSeconds(120);

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan DemonSpawnTime = TimeSpan.FromSeconds(0);

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string[] DemonSpawnIdArray = { "MobDemonSlaughter", "MobDemonHonker", "MobDemonIfrit", "MobDemonJaunt" };
}
