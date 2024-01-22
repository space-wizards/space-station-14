using Content.Server.StationEvents.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(LostPiratesSpawnRule))]
public sealed partial class LostPiratesSpawnRuleComponent : Component
{
    [DataField("lostPiratesShuttlePath")]
    public string LostPiratesShuttlePath = "Maps/Shuttles/lost_pirates.yml";

    [DataField("gameRuleProto", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string GameRuleProto = "Pirates";

    [DataField("additionalRule")]
    public EntityUid? AdditionalRule;
}
