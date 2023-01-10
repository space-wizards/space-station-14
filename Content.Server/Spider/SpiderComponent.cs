using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Spider
{
    [RegisterComponent]
    public sealed class SpiderComponent : Component
    {
        [DataField("webPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string WebPrototype = "SpiderWeb";

        [DataField("webActionName")]
        public string WebActionName = "SpiderWebAction";
    }
}
