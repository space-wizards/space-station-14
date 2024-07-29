using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared.Roles;

[Prototype]
public sealed partial class StartingGearPrototype : IPrototype, IInheritingPrototype
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
    public bool Abstract { get; }

    /// <summary>
    /// The slot and entity prototype ID of the equipment that is to be spawned and equipped onto the entity.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public Dictionary<string, EntProtoId> Equipment = new();

    /// <summary>
    /// The inhand items that are equipped when this starting gear is equipped onto an entity.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public List<EntProtoId> Inhand = new(0);

    /// <summary>
    /// Inserts entities into the specified slot's storage (if it does have storage).
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public Dictionary<string, List<EntProtoId>> Storage = new();

    /// <summary>
    /// Gets the entity prototype ID of a slot in this starting gear.
    /// </summary>
    public string GetGear(string slot)
    {
        return Equipment.TryGetValue(slot, out var equipment) ? equipment : string.Empty;
    }
}
