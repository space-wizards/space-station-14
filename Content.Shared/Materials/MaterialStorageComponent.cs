using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
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
    /// Whether or not to drop contained materials when deconstructed.
    /// </summary>
    [DataField("dropOnDeconstruct")]
    public bool DropOnDeconstruct = true;

    /// <summary>
    /// Whitelist generated on runtime for what specific materials can be inserted into this entity.
    /// </summary>
    [DataField("materialWhiteList", customTypeSerializer: typeof(PrototypeIdListSerializer<MaterialPrototype>))]
    public List<string>? MaterialWhiteList;

    /// <summary>
    /// Whether or not the visualization for the insertion animation
    /// should ignore the color of the material being inserted.
    /// </summary>
    [DataField("ignoreColor")]
    public bool IgnoreColor;

    /// <summary>
    /// The sound that plays when inserting an item into the storage
    /// </summary>
    [DataField("insertingSound")]
    public SoundSpecifier? InsertingSound;

    /// <summary>
    /// How long the inserting animation will play
    /// </summary>
    [DataField("insertionTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan InsertionTime = TimeSpan.FromSeconds(0.79f); // 0.01 off for animation timing
}

[Serializable, NetSerializable]
public enum MaterialStorageVisuals : byte
{
    Inserting
}

/// <summary>
/// event raised on the materialStorage when a material entity is inserted into it.
/// </summary>
[ByRefEvent]
public readonly record struct MaterialEntityInsertedEvent(MaterialComponent MaterialComp)
{
    public readonly MaterialComponent MaterialComp = MaterialComp;
}

/// <summary>
/// Event raised when a material amount is changed
/// </summary>
[ByRefEvent]
public readonly record struct MaterialAmountChangedEvent;

/// <summary>
/// Event raised to get all the materials that the
/// </summary>
[ByRefEvent]
public record struct GetMaterialWhitelistEvent(EntityUid Storage)
{
    public readonly EntityUid Storage = Storage;

    public List<string> Whitelist = new();
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
