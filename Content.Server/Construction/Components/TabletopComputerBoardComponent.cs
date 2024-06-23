using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Construction.Components
{
    [RegisterComponent, ComponentProtoName("TabletopComputerBoardComponent")]
    public sealed partial class TabletopComputerBoardComponent : Component
    {
        [DataField("Prototype", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? Prototype;
    }
}
