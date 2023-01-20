using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Weapons.Ranged.Components;

[RegisterComponent, NetworkedComponent]
public sealed class BallisticAmmoProviderComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("soundRack")]
    public SoundSpecifier? SoundRack = new SoundPathSpecifier("/Audio/Weapons/Guns/Cock/smg_cock.ogg");

    [ViewVariables(VVAccess.ReadWrite), DataField("soundInsert")]
    public SoundSpecifier? SoundInsert = new SoundPathSpecifier("/Audio/Weapons/Guns/MagIn/bullet_insert.ogg");

    [ViewVariables(VVAccess.ReadWrite), DataField("proto", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? FillProto;

    [ViewVariables(VVAccess.ReadWrite), DataField("capacity")]
    public int Capacity = 30;

    [ViewVariables(VVAccess.ReadWrite), DataField("unspawnedCount")]
    public int UnspawnedCount;

    [ViewVariables(VVAccess.ReadWrite), DataField("whitelist")]
    public EntityWhitelist? Whitelist;

    public Container Container = default!;

    // TODO: Make this use stacks when the typeserializer is done.
    [DataField("entities")]
    public List<EntityUid> Entities = new();

    /// <summary>
    /// Will the ammoprovider automatically cycle through rounds or does it need doing manually.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("autoCycle")]
    public bool AutoCycle = true;

    /// <summary>
    /// Is the gun ready to shoot; if AutoCycle is true then this will always stay true and not need to be manually done.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("cycled")]
    public bool Cycled = true;

    /// <summary>
    /// Is it okay for this entity to directly transfer its valid ammunition into another provider?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("mayTransfer")]
    public bool MayTransfer = false;
}
