using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Chat.TypingIndicator;

[RegisterComponent]
public sealed class TypingIndicatorClothingComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("proto", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<TypingIndicatorPrototype>))]
    public string Prototype = default!;
}
