using Content.Shared.Chat.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Wagging;

[RegisterComponent]
[Access(typeof(WaggingSystem))]
public sealed partial class WaggingComponent : Component
{
    [DataField("emote", customTypeSerializer: typeof(PrototypeIdSerializer<EmotePrototype>))]
    public string EmoteId = "WagTail";

    [ViewVariables]
    public bool Wagging = false;
}
