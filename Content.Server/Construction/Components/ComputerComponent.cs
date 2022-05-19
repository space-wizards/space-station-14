using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Construction.Components
{
    [RegisterComponent, ComponentProtoName("Computer")]
    public sealed class ComputerComponent : Component
    {
        [ViewVariables]
        [DataField("board", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? BoardPrototype;
    }
}
