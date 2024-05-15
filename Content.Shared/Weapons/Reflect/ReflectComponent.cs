using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Reflect;

/// <summary>
/// Entities with this component have a chance to reflect projectiles and hitscan shots
/// Uses <c>ItemToggleComponent</c> to control reflection.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ReflectComponent : Component
{
    /// <summary>
    /// What we reflect.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("reflects")]
    public ReflectType Reflects = ReflectType.Energy | ReflectType.NonEnergy;

    [DataField("spread"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public Angle Spread = Angle.FromDegrees(45);

    [DataField("soundOnReflect")]
    public SoundSpecifier? SoundOnReflect = new SoundPathSpecifier("/Audio/Weapons/Guns/Hits/laser_sear_wall.ogg");

    /// <summary>
    /// Is the deflection an innate power or something actively maintained? If true, this component grants a flat
    /// deflection chance rather than a chance that degrades when moving/weightless/stunned/etc.
    /// </summary>
    [DataField]
    public bool Innate = false;

    /// <summary>
    /// Maximum probability for a projectile to be reflected.
    /// </summary>
    [DataField("reflectProb"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float ReflectProb = 0.25f;

    /// <summary>
    /// The maximum velocity a wielder can move at before losing effectiveness.
    /// </summary>
    [DataField]
    public float VelocityBeforeNotMaxProb = 2.5f; // Walking speed for a human. Suitable for a weightless deflector like an e-sword.

    /// <summary>
    /// The velocity a wielder has to be moving at to use the minimum effectiveness value.
    /// </summary>
    [DataField]
    public float VelocityBeforeMinProb = 4.5f; // Sprinting speed for a human. Suitable for a weightless deflector like an e-sword.

    /// <summary>
    /// Minimum probability for a projectile to be reflected.
    /// </summary>
    [DataField]
    public float MinReflectProb = 0.1f;
}

[Flags]
public enum ReflectType : byte
{
    None = 0,
    NonEnergy = 1 << 0,
    Energy = 1 << 1,
}
