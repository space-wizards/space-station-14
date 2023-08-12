using Content.Shared.EngineerPainter.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.EngineerPainter
{
    [RegisterComponent]
    public sealed class PaintableAirlockComponent : Component
    {
        [DataField("group", customTypeSerializer:typeof(PrototypeIdSerializer<AirlockGroupPrototype>))]
        public string Group = default!;
    }
}
