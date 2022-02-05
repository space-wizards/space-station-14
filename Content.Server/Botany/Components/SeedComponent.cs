using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Botany.Components
{
    [RegisterComponent]
    public class SeedComponent : Component
    {
        [DataField("seed", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<Seed>))]
        public string SeedName = default!;
    }
}
