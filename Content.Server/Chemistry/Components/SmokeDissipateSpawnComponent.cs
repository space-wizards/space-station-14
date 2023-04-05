using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Chemistry.Components;


[RegisterComponent]
public sealed class SmokeDissipateSpawnComponent : Component
{
    [DataField("prototype", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Prototype = string.Empty;
}
