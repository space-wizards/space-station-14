using Content.Shared.Radio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Implants.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class RattleComponent : Component
{
    // The radio channel the message will be sent to
    [DataField("radioChannel", customTypeSerializer: typeof(PrototypeIdSerializer<RadioChannelPrototype>))]
    public string RadioChannel = "Syndicate";

    // The message that the implant will send when crit
    [DataField("critMessage")]
    public string CritMessage = "deathrattle-implant-critical-message";

    // The message that the implant will send when dead
    [DataField("deathMessage")]
    public string DeathMessage = "deathrattle-implant-dead-message";
}
