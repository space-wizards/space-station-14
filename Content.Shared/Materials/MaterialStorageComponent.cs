using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Materials;

[RegisterComponent, NetworkedComponent]
public sealed class MaterialStorageComponent : Component
{
    [ViewVariables]
    public Dictionary<string, int> Storage { get; set; } = new();

    /// <summary>
    ///     How much material the storage can store in total.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("storageLimit")]
    public int? StorageLimit;

    /// <summary>
    /// Whitelist for specifying the kind of items that can be insert into this entity.
    /// </summary>
    [ViewVariables]
    [DataField("whitelist")]
    public EntityWhitelist? EntityWhitelist;

    /// <summary>
    /// Whitelist generated on runtime for what specific materials can be inserted into this entity.
    /// </summary>
    [ViewVariables]
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
