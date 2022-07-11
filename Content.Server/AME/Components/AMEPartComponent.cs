using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.AME.Components
{
    [RegisterComponent]
    public sealed class AMEPartComponent : Component
    {
        [DataField("unwrapSound")]
        public SoundSpecifier UnwrapSound = new SoundPathSpecifier("/Audio/Effects/unwrap.ogg");

        [DataField("qualityNeeded", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string QualityNeeded = "Pulsing";
    }
}
