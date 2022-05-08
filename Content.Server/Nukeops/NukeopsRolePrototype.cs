using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Nukeops;

[Prototype("nukeopsRole")]
public sealed class NukeopsRolePrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    [DataField("back", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public readonly string Back = default!;
}
