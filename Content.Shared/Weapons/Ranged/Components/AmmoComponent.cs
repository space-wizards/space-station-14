using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
/// Allows the entity to be fired from a gun.
/// </summary>
[RegisterComponent, Virtual]
public partial class AmmoComponent : Component, IShootable
{
    // Muzzle flash stored on ammo because if we swap a gun to whatever we may want to override it.

    [ViewVariables(VVAccess.ReadWrite), DataField("muzzleFlash", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? MuzzleFlash = "MuzzleFlashEffect";
}

/// <summary>
/// Spawns another prototype to be shot instead of itself.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CartridgeAmmoComponent : AmmoComponent
{
    [ViewVariables(VVAccess.ReadWrite), DataField("proto", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Prototype = default!;

    [ViewVariables(VVAccess.ReadWrite), DataField("spent")]
    [AutoNetworkedField]
    public bool Spent = false;

    /// <summary>
    /// Caseless ammunition.
    /// </summary>
    [DataField("deleteOnSpawn")]
    public bool DeleteOnSpawn;

    [DataField("soundEject")]
    public SoundSpecifier? EjectSound = new SoundCollectionSpecifier("CasingEject");
}
