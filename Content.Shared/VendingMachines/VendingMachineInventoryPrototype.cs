using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.VendingMachines
{
    [Serializable, NetSerializable, Prototype("vendingMachineInventory")]
    public sealed class VendingMachineInventoryPrototype : IPrototype
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; } = default!;

        [DataField("startingInventory")]
        public Dictionary<string, uint> StartingInventory { get; } = new();

        [DataField("emaggedInventory")]
        public Dictionary<string, uint>? EmaggedInventory { get; }

        [DataField("contrabandInventory")]
        public Dictionary<string, uint>? ContrabandInventory { get; }
    }
}
