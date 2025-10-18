using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared.Roles;

[Prototype]
public sealed partial class StartingGearPrototype : IPrototype, IInheritingPrototype, IEquipmentLoadout
{
    /// <inheritdoc/>
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    /// <inheritdoc/>
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<StartingGearPrototype>))]
    public string[]? Parents { get; private set; }

    /// <inheritdoc/>
    [AbstractDataField]
    public bool Abstract { get; private set; }

    /// <inheritdoc />
    [DataField]
    [AlwaysPushInheritance]
    public Dictionary<string, EntProtoId> Equipment { get; set; } = new();

    /// <inheritdoc />
    [DataField]
    [AlwaysPushInheritance]
    public List<EntProtoId> Inhand { get; set; } = new();

    /// <inheritdoc />
    [DataField]
    [AlwaysPushInheritance]
    public Dictionary<string, List<EntProtoId>> Storage { get; set; } = new();
}

/// <summary>
/// Specifies the starting entity prototypes and where to equip them for the specified class.
/// </summary>
public interface IEquipmentLoadout
{
    /// <summary>
    /// The slot and entity prototype ID of the equipment that is to be spawned and equipped onto the entity.
    /// </summary>
    public Dictionary<string, EntProtoId> Equipment { get; set; }

    /// <summary>
    /// The inhand items that are equipped when this starting gear is equipped onto an entity.
    /// </summary>
    public List<EntProtoId> Inhand { get; set; }

    /// <summary>
    /// Inserts entities into the specified slot's storage (if it does have storage).
    /// </summary>
    public Dictionary<string, List<EntProtoId>> Storage { get; set; }

    /// <summary>
    /// Gets the entity prototype ID of a slot in this starting gear.
    /// </summary>
    public string GetGear(string slot)
    {
        return Equipment.TryGetValue(slot, out var equipment) ? equipment : string.Empty;
    }
}
