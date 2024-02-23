using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Bodycamera;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedBodyCameraSystem))]
public sealed partial class BodyCameraComponent : Component
{
    /// <summary>
    /// Is the bodycamera enabled and broadcasting
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public bool Enabled;

    [DataField]
    public SoundSpecifier? PowerOnSound = new SoundPathSpecifier("/Audio/Items/Defib/defib_success.ogg")
    {
        Params = AudioParams.Default.WithVolume(-2f).WithMaxDistance(2f)
    };

    [DataField]
    public SoundSpecifier? PowerOffSound = new SoundPathSpecifier("/Audio/Items/Defib/defib_failed.ogg")
    {
        Params = AudioParams.Default.WithVolume(-2f).WithMaxDistance(2f)
    };

    [DataField]
    public LocId UnknownUser = "bodycamera-unknown-name";

    [DataField]
    public LocId UnknownJob = "bodycamera-unknown-job";

    [DataField]
    public LocId CameraExamineOff = "bodycamera-examine-off-state";

    [DataField]
    public LocId CameraExamineOn = "bodycamera-examine-on-state";

    [DataField]
    public LocId CameraOnUse = "bodycamera-on-use";

    [DataField]
    public LocId CameraOffUse = "bodycamera-off-use";
}
