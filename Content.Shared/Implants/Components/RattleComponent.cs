using Content.Shared.Radio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Implants.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class RattleComponent : Component
{
    // The radio channel the message will be sent to
    [DataField]
    public ProtoId<RadioChannelPrototype> RadioChannel = "Syndicate";

    // The message that the implant will send when crit
    [DataField]
    public LocId CritMessage = "deathrattle-implant-critical-message";

    // The message that the implant will send when dead
    [DataField]
    public LocId DeathMessage = "deathrattle-implant-dead-message";
}
