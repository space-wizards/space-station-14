using Content.Shared.SprayPainter.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.SprayPainter;

[RegisterComponent]
public sealed partial class PaintableAirlockComponent : Component
{
    [DataField("group", customTypeSerializer:typeof(PrototypeIdSerializer<AirlockGroupPrototype>))]
    public string Group = default!;
}
