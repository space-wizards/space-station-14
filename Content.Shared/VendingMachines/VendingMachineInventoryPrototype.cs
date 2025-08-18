using Robust.Shared.Prototypes;

namespace Content.Shared.VendingMachines;

[Prototype]
public sealed partial class VendingMachineInventoryPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<EntityPrototype>, uint> StartingInventory { get; private set; } = [];

    /// <summary>
    ///
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<EntityPrototype>, uint>? EmaggedInventory { get; private set; }

    /// <summary>
    ///
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<EntityPrototype>, uint>? ContrabandInventory { get; private set; }
}
