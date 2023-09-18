using Content.Server.Fluids.EntitySystems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Chemistry.Components;


/// <summary>
/// When a <see cref="SmokeComponent"/> despawns this will spawn another entity in its place.
/// </summary>
[RegisterComponent, Access(typeof(SmokeSystem))]
public sealed partial class SmokeDissipateSpawnComponent : Component
{
    [DataField("prototype", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Prototype = string.Empty;
}
