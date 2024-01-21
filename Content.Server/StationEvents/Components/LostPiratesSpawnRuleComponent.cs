using Content.Server.StationEvents.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(LoneOpsSpawnRule))]
public sealed partial class LoneOpsSpawnRuleComponent : Component
{
    [DataField("loneOpsShuttlePath")]
    public string LoneOpsShuttlePath = "Maps/Shuttles/looser_pirates.yml";

    [DataField("gameRuleProto", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string GameRuleProto = "Pirates";

    [DataField("additionalRule")]
    public EntityUid? AdditionalRule;
}
