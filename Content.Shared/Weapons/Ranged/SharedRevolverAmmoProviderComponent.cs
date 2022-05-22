using Content.Shared.Sound;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Weapons.Ranged;

[NetworkedComponent]
public abstract class SharedRevolverAmmoProviderComponent : AmmoProviderComponent
{
    /*
     * Revolver has an array of its slots of which we can fire from any index.
     * We also keep a separate array of slots we haven't spawned entities for, Chambers. This means that rather than creating
     * for example 7 entities when revolver spawns (1 for the revolver and 6 cylinders) we can instead defer it.
     */

    //[ViewVariables, DataField("whitelist", required: true)]
    //public EntityWhitelist Whitelist = default!;

    public Container AmmoContainer = default!;

    [ViewVariables, DataField("currentSlot")]
    public int CurrentIndex;

    [ViewVariables, DataField("capacity")]
    public int Capacity = 6;

    [DataField("ammoSlots", readOnly: true)]
    public EntityUid?[] AmmoSlots = Array.Empty<EntityUid?>();

    public bool?[] Chambers = Array.Empty<bool?>();

    [DataField("fillProto", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? FillPrototype = "BulletMagnum";

    [ViewVariables, DataField("soundEject")]
    public SoundSpecifier SoundEject = new SoundPathSpecifier("/Audio/Weapons/Guns/MagOut/revolver_magout.ogg");

    [ViewVariables, DataField("soundInsert")]
    public SoundSpecifier SoundInsert = new SoundPathSpecifier("/Audio/Weapons/Guns/MagIn/revolver_magin.ogg");

    [ViewVariables, DataField("soundSpin")]
    public SoundSpecifier SoundSpin = new SoundPathSpecifier("/Audio/Weapons/Guns/Misc/revolver_spin.ogg");
}
