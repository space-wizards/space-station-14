#nullable enable
using System;
using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using YamlDotNet.RepresentationModel;

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
        private string _id = string.Empty;
        private List<string> _inventory = new();

        public string ID => _id;
        public List<string> Inventory => _inventory;

        public void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);

            serializer.DataField(ref _id, "id", string.Empty);
            serializer.DataField(ref _inventory, "inventory", new List<string>());
        }
    }
}
