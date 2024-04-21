using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Reflect;

/// <summary>
/// Entities with this component have a chance to reflect projectiles and hitscan shots
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ReflectComponent : Component
{
    /// <summary>
    /// Can only reflect when enabled
    /// </summary>
    [DataField("enabled"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    /// What we reflect.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("reflects")]
    public ReflectType Reflects = ReflectType.Energy | ReflectType.NonEnergy;

    /// <summary>
    /// Probability for a projectile to be reflected.
    /// </summary>
    [DataField("reflectProb"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float ReflectProb = 0.25f;

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
    /// The multiplier used when moving.
    /// </summary>
    [DataField]
    public float MovingProbMultiplier = 0.66f;

    /// <summary>
    /// The multiplier used when sprinting or when otherwise compromised (e.g. weightless)
    /// </summary>
    [DataField]
    public float SprintingProbMultiplier = 0.33f;
}

[Flags]
public enum ReflectType : byte
{
    None = 0,
    NonEnergy = 1 << 0,
    Energy = 1 << 1,
}
