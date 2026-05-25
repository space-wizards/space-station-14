using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Misc;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ForceGunComponent : Component
{
    [DataField("throwSpeed"), AutoNetworkedField]
    public float ThrowSpeed = 10f;

    [DataField("pushBackRatio"), AutoNetworkedField]
    public float PushBackRatio = 1f;

    [DataField("soundLaunch")]
    public SoundSpecifier? LaunchSound = new SoundPathSpecifier("/Audio/Weapons/soup.ogg")
    {
        Params = AudioParams.Default.WithVolume(5f),
    };
}
