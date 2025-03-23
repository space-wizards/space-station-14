using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Weapons.Ranged.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class RevolverAmmoProviderComponent : AmmoProviderComponent
{
    /*
     * Revolver has an array of its slots of which we can fire from any index.
     * We also keep a separate array of slots we haven't spawned entities for, Chambers. This means that rather than creating
     * for example 7 entities when revolver spawns (1 for the revolver and 6 cylinders) we can instead defer it.
     */

    [DataField]
    public EntityWhitelist? Whitelist;

    public Container AmmoContainer = default!;

    [DataField]
    public int CurrentIndex;

    [DataField]
    public int Capacity = 6;

    // Like BallisticAmmoProvider we defer spawning until necessary
    // AmmoSlots is the instantiated ammo and Chambers is the unspawned ammo (that may or may not have been shot).

    // TODO: Using an array would be better but this throws!
    [DataField]
    public List<EntityUid?> AmmoSlots;

    /// <summary>
    /// Bool array for chambers. Every bool can be null, true or false.
    /// Null will create empty chamber.
    /// True will create chamber with <see cref="FillPrototype"/>.
    /// False will create chamber with used ammo.
    /// </summary>
    [DataField]
    public bool?[] Chambers = [];

    /// <summary>
    /// Prototype id that will be used as started ammo. Null will create entity with empty magazine.
    /// </summary>
    [DataField("proto", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? FillPrototype = "CartridgeMagnum";

    [DataField]
    public SoundSpecifier? SoundEject = new SoundPathSpecifier("/Audio/Weapons/Guns/MagOut/revolver_magout.ogg");

    [DataField]
    public SoundSpecifier? SoundInsert = new SoundPathSpecifier("/Audio/Weapons/Guns/MagIn/revolver_magin.ogg");

    [DataField]
    public SoundSpecifier? SoundSpin = new SoundPathSpecifier("/Audio/Weapons/Guns/Misc/revolver_spin.ogg");
}
