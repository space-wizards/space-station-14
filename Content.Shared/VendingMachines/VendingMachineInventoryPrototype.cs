using System.Linq;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.VendingMachines;

[Prototype]
public sealed partial class VendingMachineInventoryPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public List<VendingMachineInventoryCategory> Categories { get; private set; } = [];

    /// <summary>
    /// Enumerates inventory entries of the specified type across all categories.
    /// </summary>
    public IEnumerable<KeyValuePair<EntProtoId, uint>> EnumerateInventory(InventoryType type)
    {
        return Categories.SelectMany(category => category.GetInventory(type));
    }
}

/// <summary>
/// Contains the inventory entries belonging to one vending machine category.
/// </summary>
[DataDefinition]
public sealed partial class VendingMachineInventoryCategory
{
    [DataField]
    public LocId? Name { get; private set; }

    [DataField]
    public SpriteSpecifier? Icon { get; private set; }

    [DataField]
    public Dictionary<EntProtoId, uint> StartingInventory { get; private set; } = [];

    [DataField]
    public Dictionary<EntProtoId, uint> EmaggedInventory { get; private set; } = [];

    [DataField]
    public Dictionary<EntProtoId, uint> ContrabandInventory { get; private set; } = [];

    public IReadOnlyDictionary<EntProtoId, uint> GetInventory(InventoryType type)
    {
        return type switch
        {
            InventoryType.Regular => StartingInventory,
            InventoryType.Emagged => EmaggedInventory,
            InventoryType.Contraband => ContrabandInventory,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}
