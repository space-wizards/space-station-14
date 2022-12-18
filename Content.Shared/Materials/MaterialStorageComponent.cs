using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Materials;

[Access(typeof(SharedMaterialStorageSystem))]
[RegisterComponent, NetworkedComponent]
public sealed class MaterialStorageComponent : Component
{
    [DataField("storage", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<int, MaterialPrototype>))]
    public Dictionary<string, int> Storage { get; set; } = new();

    /// <summary>
    ///     How much material the storage can store in total.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("storageLimit")]
    public int? StorageLimit;

    /// <summary>
    /// Whitelist for specifying the kind of items that can be insert into this entity.
    /// </summary>
    [DataField("whitelist")]
    public EntityWhitelist? EntityWhitelist;

    /// <summary>
    /// Whitelist generated on runtime for what specific materials can be inserted into this entity.
    /// </summary>
    [DataField("materialWhiteList", customTypeSerializer: typeof(PrototypeIdListSerializer<MaterialPrototype>))]
    public List<string>? MaterialWhiteList;

    /// <summary>
    /// The sound that plays when inserting an item into the storage
    /// </summary>
    [DataField("insertingSound")]
    public SoundSpecifier? InsertingSound;
}

/// <summary>
/// event raised on the materialStorage when a material entity is inserted into it.
/// </summary>
public readonly struct MaterialEntityInsertedEvent
{
    public readonly Dictionary<string, int> Materials;

    public MaterialEntityInsertedEvent(Dictionary<string, int> materials)
    {
        Materials = materials;
    }
}

/// <summary>
/// Event raised when a material amount is changed
/// </summary>
public readonly struct MaterialAmountChangedEvent
{

}

public sealed class GetMaterialWhitelistEvent : EntityEventArgs
{
    public readonly EntityUid Storage;

    public List<string> Whitelist = new();

    public GetMaterialWhitelistEvent(EntityUid storage)
    {
        Storage = storage;
    }
}

[Serializable, NetSerializable]
public sealed class MaterialStorageComponentState : ComponentState
{
    public Dictionary<string, int> Storage;

    public List<string>? MaterialWhitelist;

    public MaterialStorageComponentState(Dictionary<string, int> storage, List<string>? materialWhitelist)
    {
        Storage = storage;
        MaterialWhitelist = materialWhitelist;
    }
}
