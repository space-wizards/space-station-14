using Content.Shared.Chat.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Wagging;

[RegisterComponent, NetworkedComponent]
public sealed partial class WaggingComponent : Component
{
    [DataField("emote", customTypeSerializer: typeof(PrototypeIdSerializer<EmotePrototype>))]
    public string EmoteId = "WagTail";

    [ViewVariables]
    public bool Wagging = false;
}
