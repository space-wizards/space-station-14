using Content.Shared.Sound;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
/// Allows the entity to be fired from a gun.
/// </summary>
[RegisterComponent, Virtual]
public class AmmoComponent : Component, IShootable
{
    // Muzzle flash stored on ammo because if we swap a gun to whatever we may want to override it.

    [ViewVariables, DataField("muzzleFlash")]
    public ResourcePath? MuzzleFlash = new ResourcePath("Objects/Weapons/Guns/Projectiles/projectiles.rsi/muzzle_bullet.png");
}

/// <summary>
/// Spawns another prototype to be shot instead of itself.
/// </summary>
[RegisterComponent, NetworkedComponent, ComponentReference(typeof(AmmoComponent))]
public sealed class CartridgeAmmoComponent : AmmoComponent
{
    [ViewVariables(VVAccess.ReadWrite), DataField("proto", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Prototype = default!;

    [ViewVariables(VVAccess.ReadWrite), DataField("spent")]
    public bool Spent = false;

    /// <summary>
    /// How much the ammo spreads when shot, in degrees. Does nothing if count is 0.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("spread")]
    public Angle Spread = Angle.FromDegrees(5);

    /// <summary>
    /// How many prototypes are spawned when shot.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("count")]
    public int Count = 1;

    /// <summary>
    /// Caseless ammunition.
    /// </summary>
    [ViewVariables, DataField("deleteOnSpawn")]
    public bool DeleteOnSpawn;

    [ViewVariables, DataField("soundEject")]
    public SoundSpecifier? EjectSound = new SoundCollectionSpecifier("CasingEject");
}
