using Content.Shared.AirlockPainter.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.AirlockPainter
{
    [RegisterComponent]
    public sealed class PaintableAirlockComponent : Component
    {
        [DataField("group", customTypeSerializer:typeof(PrototypeIdSerializer<AirlockGroupPrototype>))]
        public string Group = default!;
    }
}
