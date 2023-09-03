using Content.Server.StationEvents.Events;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(LoneOpsSpawnRule))]
public sealed partial class LoneOpsSpawnRuleComponent : Component
{
    [DataField("loneOpsShuttlePath")]
    public string LoneOpsShuttlePath = "Maps/Shuttles/striker.yml";

    [DataField("gameRuleProto", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string GameRuleProto = "Nukeops";

    [DataField("additionalRule")]
    public EntityUid? AdditionalRule;

    // SS220 Lone-Nukie-Declare-War begin
    public MapId? ShuttleOriginMap;

    [DataField("warTCAmount")]
    public int WarTCAmount = 100;

    //Waiting 20 minutes on a crapmed shuttle would make you go insane
    // so i cut it down to 10
    [DataField("warArriveDelay")]
    public TimeSpan? WarArriveDelay = TimeSpan.FromMinutes(10);
    // SS220 Lone-Nukie-Declare-War end
}
