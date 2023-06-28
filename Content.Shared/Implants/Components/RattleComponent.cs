using Content.Shared.Radio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Implants.Components;

[RegisterComponent]
public sealed class RattleComponent : Component
{
    // The radio channel the message will be sent to
    [DataField("radioChannel", customTypeSerializer: typeof(PrototypeIdSerializer<RadioChannelPrototype>))]
    public string RadioChannel = "Syndicate";

    // The message that the implant will send
    [DataField("message")]
    public string Message = "";
}
