using System;
using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.GameObjects.Components.Chemistry
{
    [Serializable, NetSerializable, Prototype("reagentDispenserInventory")]
    public class ReagentDispenserInventoryPrototype : IPrototype, IIndexedPrototype
    {
        private string _id;
        private List<string> _inventory;

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
