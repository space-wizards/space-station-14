// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Storage;

namespace Content.Shared.DeadSpace.Spiders.SpiderInfector.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class SpiderInfectorComponent : Component
{
    [DataField]
    public EntProtoId SpiderInfectorAction = "ActionSpiderInfector";

    [DataField, AutoNetworkedField]
    public EntityUid? SpiderInfectorActionEntity;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public float InfectedDuration = 2.5f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public float HealDuration = 12f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public float InfectDuration = 300f;

    [DataField("spawned", required: true)]
    public List<EntitySpawnEntry> SpawnedEntities = new();

}
