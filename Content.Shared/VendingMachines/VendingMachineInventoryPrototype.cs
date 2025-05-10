using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.VendingMachines
{
    [DataDefinition, Serializable]
    public sealed partial class VendingMachineInventoryEntryForPrototype
    {
        [DataField("id", required: true)]
        public string ID = default!;

        [DataField("amount")]
        public uint Amount = 1;

        [DataField("price")]
        public int Price = 0; // Optional; use default price if null
    }

    [Serializable, NetSerializable, Prototype]
    public sealed partial class VendingMachineInventoryPrototype : IPrototype
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; private set; } = default!;

        [DataField("startingInventory")]
        public List<VendingMachineInventoryEntryForPrototype> StartingInventory { get; private set; } = new();

        [DataField("emaggedInventory")]
        public List<VendingMachineInventoryEntryForPrototype>? EmaggedInventory { get; private set; }

        [DataField("contrabandInventory")]
        public List<VendingMachineInventoryEntryForPrototype>? ContrabandInventory { get; private set; }
    }

}
