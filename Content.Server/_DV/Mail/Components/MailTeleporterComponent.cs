using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._DV.Mail.Components;

/// <summary>
/// This is for the mail teleporter.
/// Random mail will be teleported to this every few minutes.
/// </summary>
[RegisterComponent]
public sealed partial class MailTeleporterComponent : Component
{
    // Not starting accumulator at 0 so mail carriers have some deliveries to make shortly after roundstart.
    [DataField]
    public float Accumulator = 285f;

    /// <summary>
    /// The sound that's played when new mail arrives.
    /// </summary>
    [DataField]
    public SoundSpecifier TeleportSound = new SoundPathSpecifier("/Audio/Effects/teleport_arrival.ogg");

    /// <summary>
    /// Imp : The VFX spawned when mail teleports in.
    /// </summary>
    [DataField]
    public EntProtoId BeamInFx = "MailTelepadVFX";
    /// <summary>
    /// The MailDeliveryPoolPrototype that's used to select what mail this
    /// teleporter can deliver.
    /// </summary>
    [DataField]
    public string MailPool = "RandomDeltaVMailDeliveryPool"; // Frontier / DeltaV: Mail rework
}
