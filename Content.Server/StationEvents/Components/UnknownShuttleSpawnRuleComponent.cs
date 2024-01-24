using Content.Server.StationEvents.Events;
using Robust.Shared.Prototypes;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(UnknownShuttleSpawnRule))]
public sealed partial class UnknownShuttleSpawnRuleComponent : Component
{
    [DataField]
    public string ShuttlePath = "Maps/Shuttles/shittle.yml";

    [DataField]
    public EntProtoId? GameRuleProto;

    [DataField]
    public EntityUid? SpawnedGameRule;
}
