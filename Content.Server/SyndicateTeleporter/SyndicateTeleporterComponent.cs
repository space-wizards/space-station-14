using Robust.Shared.Audio;

namespace Content.Server.SyndicateTeleporter;

[RegisterComponent]
public sealed partial class SyndicateTeleporterComponent : Component
{
    [DataField("RandomValue")]
    [ViewVariables(VVAccess.ReadWrite)]
    public int RandomValue = 4;

    [DataField("TeleportationValue")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float TeleportationValue = 4f;

    [DataField("SaveAttempts")]
    [ViewVariables(VVAccess.ReadWrite)]
    public int SaveAttempts = 1;

    [DataField("SaveDistance")]
    [ViewVariables(VVAccess.ReadWrite)]
    public int SaveDistance = 3;

    [ViewVariables(VVAccess.ReadWrite), DataField("alarm"), AutoNetworkedField]
    public SoundSpecifier? AlarmSound = new SoundPathSpecifier("/Audio/Effects/beeps.ogg");

}
