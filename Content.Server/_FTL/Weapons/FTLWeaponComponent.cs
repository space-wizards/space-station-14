using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._FTL.Weapons;

/// <summary>
/// This is used for tracking weapons.
/// </summary>
[RegisterComponent]
public sealed class FTLWeaponComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)] public bool CanBeUsed = true;

    [DataField("cooldownTime"), ViewVariables(VVAccess.ReadOnly)]
    public float CooldownTime = 1f;

    [DataField("fireSound"), ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier FireSound = new SoundPathSpecifier("/Audio/Weapons/Guns/Gunshots/rpgfire.ogg");

    [DataField("prototype", customTypeSerializer: typeof(PrototypeIdSerializer<FTLAmmoType>))]
    public string Prototype { get; set; } = "";

    [DataField("cooldownSound"), ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier CooldownSound = new SoundPathSpecifier("/Audio/Weapons/click.ogg");

}

/// <summary>
/// Used for tracking that the weapon is on a cooldown.
/// </summary>
[RegisterComponent]
public sealed class FTLActiveCooldownWeaponComponent : Component
{
    public float SecondsLeft = 0f;
}

