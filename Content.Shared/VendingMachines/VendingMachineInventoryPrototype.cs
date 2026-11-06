using System.Linq;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.VendingMachines;

[Prototype]
public sealed partial class VendingMachineInventoryPrototype : IPrototype
{
    /// <summary>
    /// Prototype ID of this inventory pack.
    /// </summary>
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Categories configured for this inventory pack.
    /// </summary>
    [DataField(required: true)]
    public List<VendingMachineInventoryCategory> Categories { get; private set; } = [];

    /// <summary>
    /// Enumerates inventory entries of the specified type across all categories.
    /// </summary>
    public IEnumerable<KeyValuePair<EntProtoId, uint>> EnumerateInventory(InventoryType type) =>
        Categories.SelectMany(category => category.GetInventory(type));
}

/// <summary>
/// Contains the inventory entries belonging to one vending machine category.
/// </summary>
[DataDefinition]
public sealed partial class VendingMachineInventoryCategory
{
    /// <summary>
    /// Localized name shown for this category.
    /// </summary>
    [DataField(required: true)]
    public LocId Name { get; private set; }

    /// <summary>
    /// Icon shown on the category button.
    /// </summary>
    [DataField(required: true)]
    public SpriteSpecifier Icon { get; private set; } = default!;

    /// <summary>
    /// Items stocked normally, with their starting amounts.
    /// </summary>
    [DataField]
    public Dictionary<EntProtoId, uint> StartingInventory { get; private set; } = [];

    /// <summary>
    /// Items available when the machine is emagged, with their starting amounts.
    /// </summary>
    [DataField]
    public Dictionary<EntProtoId, uint> EmaggedInventory { get; private set; } = [];

    /// <summary>
    /// Items available when contraband is unlocked, with their starting amounts.
    /// </summary>
    [DataField]
    public Dictionary<EntProtoId, uint> ContrabandInventory { get; private set; } = [];

    /// <summary>
    /// Gets this category's entries for the specified inventory type.
    /// </summary>
    public IReadOnlyDictionary<EntProtoId, uint> GetInventory(InventoryType type) =>
        type switch
        {
            InventoryType.Regular => StartingInventory,
            InventoryType.Emagged => EmaggedInventory,
            InventoryType.Contraband => ContrabandInventory,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
}
