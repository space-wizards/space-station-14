using Content.Shared.OmniPainter.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.OmniPainter
{
    [RegisterComponent]
    public sealed class PaintableAirlockComponent : Component
    {
        [DataField("group", customTypeSerializer:typeof(PrototypeIdSerializer<AirlockGroupPrototype>))]
        public string Group = default!;
    }
}
