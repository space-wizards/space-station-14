using Robust.Shared.Audio;

namespace Content.Server.SyndicateTeleporter;

[RegisterComponent]
public sealed partial class SyndicateTeleporterComponent : Component
{
    [DataField("randomValue")]
    [ViewVariables(VVAccess.ReadWrite)]
    public int RandomValue = 4;

    [DataField("teleportationValue")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float TeleportationValue = 4f;

    [DataField("saveAttempts")]
    [ViewVariables(VVAccess.ReadWrite)]
    public int SaveAttempts = 1;

    [DataField("saveDistance")]
    [ViewVariables(VVAccess.ReadWrite)]
    public int SaveDistance = 3;

    [ViewVariables(VVAccess.ReadWrite), DataField("alarm"), AutoNetworkedField]
    public SoundSpecifier? AlarmSound = new SoundPathSpecifier("/Audio/Effects/beeps.ogg");

}
