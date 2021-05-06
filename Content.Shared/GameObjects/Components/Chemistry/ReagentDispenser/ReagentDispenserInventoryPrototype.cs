#nullable enable
using System;
using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Chemistry.ReagentDispenser
{
    /// <summary>
    /// Is simply a list of reagents defined in yaml. This can then be set as a
    /// <see cref="ReagentDispenserComponent"/>s <c>pack</c> value (also in yaml),
    /// to define which reagents it's able to dispense. Based off of how vending
    /// machines define their inventory.
    /// </summary>
    [Serializable, NetSerializable, Prototype("reagentDispenserInventory")]
    public class ReagentDispenserInventoryPrototype : IPrototype
    {
        [DataField("inventory")]
        private List<string> _inventory = new();

        [ViewVariables]
        [DataField("id", required: true)]
        public string ID { get; } = default!;

        public List<string> Inventory => _inventory;
    }
}
