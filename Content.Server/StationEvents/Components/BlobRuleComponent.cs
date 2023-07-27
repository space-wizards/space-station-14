using Content.Server.StationEvents.Events;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(BlobSpawnRule))]
public sealed class BlobSpawnRuleComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly), DataField("spawnPointProto")]
    public string SpawnPointProto = "SpawnPointGhostBlob";
}
