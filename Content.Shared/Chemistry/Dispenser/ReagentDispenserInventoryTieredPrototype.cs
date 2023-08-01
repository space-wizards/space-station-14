using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Chemistry.Dispenser
{
    /// <summary>
    /// This is used to define a list of reagents that a machine can dispense
    /// when its machine parts have all be upgraded to the specified tier.
    /// </summary>
    [Serializable, NetSerializable, Prototype("reagentDispenserInventoryTiered")]
    public sealed class ReagentDispenserInventoryTieredPrototype : IPrototype
    {
        [DataField("inventory", customTypeSerializer: typeof(PrototypeIdListSerializer<ReagentPrototype>))]
        private List<string> _inventory = new();

        [DataField("tier")]
        public int? Tier { get; set; }

        [ViewVariables]
        [IdDataField]
        public string ID { get; } = default!;

        public List<string> Inventory => _inventory;
    }
}
