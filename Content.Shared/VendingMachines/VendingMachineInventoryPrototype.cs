using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared.VendingMachines;

[Prototype]
public sealed partial class VendingMachineInventoryPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public Dictionary<ProtoId<EntityPrototype>, uint> StartingInventory { get; private set; } = [];

    [DataField]
    public Dictionary<ProtoId<EntityPrototype>, uint>? EmaggedInventory { get; private set; }

    [DataField]
    public Dictionary<ProtoId<EntityPrototype>, uint>? ContrabandInventory { get; private set; }
}
