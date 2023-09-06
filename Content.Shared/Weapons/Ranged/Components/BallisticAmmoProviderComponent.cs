using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Weapons.Ranged.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class BallisticAmmoProviderComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("soundRack")]
    public SoundSpecifier? SoundRack = new SoundPathSpecifier("/Audio/Weapons/Guns/Cock/smg_cock.ogg");

    [ViewVariables(VVAccess.ReadWrite), DataField("soundInsert")]
    public SoundSpecifier? SoundInsert = new SoundPathSpecifier("/Audio/Weapons/Guns/MagIn/bullet_insert.ogg");

    [ViewVariables(VVAccess.ReadWrite), DataField("proto", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? FillProto;

    [ViewVariables(VVAccess.ReadWrite), DataField("capacity")]
    public int Capacity = 30;

    public int Count => UnspawnedCount + Container.ContainedEntities.Count;

    [ViewVariables(VVAccess.ReadWrite), DataField("unspawnedCount")]
    public int UnspawnedCount;

    [ViewVariables(VVAccess.ReadWrite), DataField("whitelist")]
    public EntityWhitelist? Whitelist;

    public Container Container = default!;

    // TODO: Make this use stacks when the typeserializer is done.
    [DataField("entities")]
    public List<EntityUid> Entities = new();

    /// <summary>
    /// Is the magazine allowed to be manually cycled to eject a cartridge.
    /// </summary>
    /// <remarks>
    /// Set to false for entities like turrets to avoid users being able to cycle them.
    /// </remarks>
    [ViewVariables(VVAccess.ReadWrite), DataField("cycleable")]
    public bool Cycleable = true;

    /// <summary>
    /// Is it okay for this entity to directly transfer its valid ammunition into another provider?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("mayTransfer")]
    public bool MayTransfer = false;

    /// <summary>
    /// DoAfter delay for filling a bullet into another ballistic ammo provider.
    /// </summary>
    [DataField("fillDelay")]
    public TimeSpan FillDelay = TimeSpan.FromSeconds(0.5);
}
