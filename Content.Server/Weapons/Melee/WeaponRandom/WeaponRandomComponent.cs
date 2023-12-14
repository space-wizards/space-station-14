using Content.Shared.Damage;
using Robust.Shared.Audio;

namespace Content.Server.Weapons.Melee.WeaponRandom;

[RegisterComponent]
internal sealed partial class WeaponRandomComponent : Component
{

    /// <summary>
    /// Amount of damage that will be caused. This is specified in the yaml.
    /// </summary>
    [DataField("damageBonus")]
    public DamageSpecifier DamageBonus = new();

    /// <summary>
    /// Chance for the damage bonus to occur.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float RandomDamageChance = 0.00001f;

    /// <summary>
    /// If this is true then the random damage will occur.
    /// </summary>
    [DataField("randomDamage")]
    public bool RandomDamage = true;

    /// <summary>
    /// If this is true then the weapon will have a unique interaction with cluwnes.
    /// </summary>
    [DataField("antiCluwne")]
    public bool AntiCluwne = true;

    /// <summary>
    /// Noise to play when the damage bonus occurs.
    /// </summary>
    [DataField("damageSound")]
    public SoundSpecifier DamageSound = new SoundPathSpecifier("/Audio/Items/bikehorn.ogg");

}
