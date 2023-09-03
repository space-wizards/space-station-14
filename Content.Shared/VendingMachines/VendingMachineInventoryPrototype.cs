using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared.VendingMachines
{
    [Serializable, NetSerializable, Prototype("vendingMachineInventory")]
    public sealed class VendingMachineInventoryPrototype : IPrototype
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; } = default!;

        [DataField("inventory",
            customTypeSerializer: typeof(PrototypeIdDictionarySerializer<uint, EntityPrototype>))]
        public Dictionary<string, uint> Inventory { get; private set; } = new();

        [DataField("inventoryType",
            customTypeSerializer: typeof(PrototypeIdSerializer<VendingMachineInventoryTypePrototype>))]
        public string InventoryTypePrototypeId { get; private set; } = "RegularInventory";
    }
}
