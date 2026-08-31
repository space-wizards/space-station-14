using Robust.Shared.Prototypes;

namespace Content.Shared.VendingMachines;

[Prototype]
public sealed partial class VendingMachineInventoryPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public Dictionary<EntProtoId, uint> StartingInventory { get; private set; } = new();

    [DataField]
    public Dictionary<EntProtoId, uint>? EmaggedInventory { get; private set; }

    [DataField]
    public Dictionary<EntProtoId, uint>? ContrabandInventory { get; private set; }
}
