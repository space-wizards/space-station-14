using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.ViewVariables;

namespace Content.Server.Computer
{
    [RegisterComponent, ComponentProtoName("Computer")]
    public sealed class ComputerComponent : Component
    {
        [ViewVariables]
        [DataField("board", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? BoardPrototype;
    }
}
