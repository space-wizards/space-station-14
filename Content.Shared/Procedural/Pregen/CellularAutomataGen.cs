using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Procedural.Pregen;

[DataDefinition]
public sealed class CellularAutomataGen : IPregen
{
    [DataField("entity", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Entity = string.Empty;
}
