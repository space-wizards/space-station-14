using System;
using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.VendingMachines
{
    [Serializable, NetSerializable, Prototype("vendingMachineInventory")]
    public class VendingMachineInventoryPrototype : IPrototype
    {
        private string _id;
        private string _name;
        private string _description;
        private double _animationDuration;
        private string _spriteName;
        private Dictionary<string, uint> _startingInventory;

        public string ID => _id;
        public string Name => _name;
        public string Description => _description;
        public double AnimationDuration => _animationDuration;
        public string SpriteName => _spriteName;
        public Dictionary<string, uint> StartingInventory => _startingInventory;

        public void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);

            serializer.DataField(ref _id, "id", string.Empty);
            serializer.DataField(ref _name, "name", string.Empty);
            serializer.DataField(ref _description, "description", string.Empty);
            serializer.DataField<double>(ref _animationDuration, "animationDuration", 0);
            serializer.DataField(ref _spriteName, "spriteName", string.Empty);
            serializer.DataField(ref _startingInventory, "startingInventory", new Dictionary<string, uint>());
        }
    }
}
