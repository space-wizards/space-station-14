// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;

namespace Content.Shared.DeadSpace.Demons.Herald.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class HeraldGhostComponent : Component
{
    [DataField]
    public string ActionHeraldSpawn = "ActionHeraldSpawn";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionHeraldSpawnEntity;

    [DataField]
    public string HeraldMobSpawnId = "MobDemonHerald";

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public string DemonPortalSpawnId = "DemonPortal";
}
