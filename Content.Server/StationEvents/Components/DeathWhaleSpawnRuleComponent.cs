using Content.Server.StationEvents.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;


namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(OceanSpawnRule))]
public sealed partial class OceanSpawnRuleComponent : Component
{
    /// <summary>
    /// The entity to be spawned.
    /// </summary>
    [DataField("prototype", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Prototype = string.Empty;

    [DataField("amount")]
    public float Amount = 5;

    [DataField("currentAmount")]
    public float CurrentAmount = 0;
}
