using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Misc;

[RegisterComponent, NetworkedComponent]
public sealed partial class ForceGunComponent : Component
{
    /// <summary>
    /// Speed an object launched will have
    /// </summary>
    [DataField("throwSpeed")]
    public float ThrowSpeed = 20f;

    [DataField("soundLaunch")]
    public SoundSpecifier? LaunchSound = new SoundPathSpecifier("/Audio/Weapons/soup.ogg")
    {
        Params = AudioParams.Default.WithVolume(5f),
    };
}
