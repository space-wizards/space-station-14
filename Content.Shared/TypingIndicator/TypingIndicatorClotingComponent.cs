using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.TypingIndicator;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedTypingIndicatorSystem))]
public sealed partial class TypingIndicatorClothingComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("prototype", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<TypingIndicatorPrototype>))]
    public string Prototype = default!;
}
