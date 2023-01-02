using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Chemistry.Dispenser
{
    /// <summary>
    /// Is simply a list of reagents defined in yaml. This can then be set as a
    /// <see cref="SharedReagentDispenserComponent"/>s <c>pack</c> value (also in yaml),
    /// to define which reagents it's able to dispense. Based off of how vending
    /// machines define their inventory.
    /// </summary>
    [Serializable, NetSerializable, Prototype("reagentDispenserInventory")]
    public sealed partial class ReagentDispenserInventoryPrototype : IPrototype
    {
        [ViewVariables, IdDataField]
        public string ID { get; } = default!;

        [DataField("inventory", customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
        private List<string> _inventory = new();

        [DataField("labels", customTypeSerializer: typeof(PrototypeIdListSerializer<ReagentPrototype>))]
        private List<string> _labels = new();

        public List<string> Inventory => _inventory;
        public List<string> Labels => _labels;
    }
}
