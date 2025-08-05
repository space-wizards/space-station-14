using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Misc;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ForceGunComponent : BaseForceGunComponent
{
    /// <summary>
    /// Maximum distance to throw entities.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ThrowDistance = 15f;

    [DataField, AutoNetworkedField]
    public float ThrowForce = 30f;

    [DataField("soundLaunch")]
    public SoundSpecifier? LaunchSound = new SoundPathSpecifier("/Audio/Weapons/soup.ogg")
    {
        Params = AudioParams.Default.WithVolume(5f),
    };
}
