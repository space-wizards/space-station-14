using Content.Server.StationEvents.Events;
using Content.Shared.GridPreloader.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(UnknownShuttleSpawnRule))]
public sealed partial class UnknownShuttleSpawnRuleComponent : Component
{
    [DataField]
    public List<ProtoId<PreloadedGridPrototype>>? ShuttleVariants = new();

    [DataField]
    public EntProtoId? GameRuleProto;

    [DataField]
    public EntityUid? SpawnedGameRule;
}
