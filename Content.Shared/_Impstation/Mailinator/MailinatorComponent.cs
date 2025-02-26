using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Mailinator;

[RegisterComponent]
public sealed partial class MailinatorComponent : Component
{
    // TODO: yml-definable whitelist of valid targets instead of MailinatorTargetComponent.

    /// <summary>
    /// The verb. Duh.
    /// This should be a LocId but my adderall is wearing off.
    /// </summary>
    [DataField]
    public string VerbText = "Send to nearest Mail Telepad";

    /// <summary>
    /// The VFX spawned when the verb is used.
    /// </summary>
    [DataField]
    public EntProtoId BeamInFx = "MailTelepadVFX";

    /// <summary>
    /// Length of the doafter.
    /// </summary>
    [DataField]
    public TimeSpan DoAfterLength = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The sounds that are played centered at the mailinator and its target respectively when the verb is used.
    /// </summary>
    [DataField]
    public SoundSpecifier DepartureSound = new SoundPathSpecifier("/Audio/Effects/teleport_departure.ogg");
    [DataField]
    public SoundSpecifier ArrivalSound = new SoundPathSpecifier("/Audio/Effects/teleport_arrival.ogg");
}
