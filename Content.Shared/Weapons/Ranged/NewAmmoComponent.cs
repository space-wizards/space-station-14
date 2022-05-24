using Content.Shared.Sound;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;

namespace Content.Shared.Weapons.Ranged;

/// <summary>
/// Allows the entity to be fired from a gun.
/// </summary>
[RegisterComponent, Virtual]
public class NewAmmoComponent : Component, SharedNewGunSystem.IShootable
{
    // Muzzle flash stored on ammo because if we swap a gun to whatever we may want to override it.

    [ViewVariables, DataField("muzzleFlash")]
    public ResourcePath? MuzzleFlash = new("Objects/Weapons/Guns/Projectiles/bullet_muzzle.png");
}

/// <summary>
/// Spawns another prototype to be shot instead of itself.
/// </summary>
[RegisterComponent, ComponentReference(typeof(NewAmmoComponent))]
public sealed class CartridgeAmmoComponent : NewAmmoComponent
{
    [ViewVariables, DataField("proto", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Prototype = default!;

    [ViewVariables, DataField("spent")]
    public bool Spent = false;

    /// <summary>
    /// Caseless ammunition.
    /// </summary>
    [ViewVariables, DataField("deleteOnSpawn")]
    public bool DeleteOnSpawn = false;

    [ViewVariables, DataField("soundEject")]
    public SoundSpecifier? EjectSound = new SoundCollectionSpecifier("CasingEject");
}
