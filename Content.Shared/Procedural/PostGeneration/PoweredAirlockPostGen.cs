using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Procedural.PostGeneration;

/// <summary>
/// Places doors with APC cables underneath them between rooms.
/// </summary>
public sealed class PoweredAirlockPostGen : IPostDunGen
{
    [DataField("door", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Door = "AirlockGlass";
}
