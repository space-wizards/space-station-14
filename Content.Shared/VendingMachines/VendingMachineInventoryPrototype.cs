using System;
using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.VendingMachines
{
    [Serializable, NetSerializable, Prototype("vendingMachineInventory")]
    public class VendingMachineInventoryPrototype : IPrototype
    {
        [DataField("id")] private string _id;
        [DataField("name")] private string _name;
        [DataField("description")] private string _description;
        [DataField("animationDuration")] private double _animationDuration;
        [DataField("spriteName")] private string _spriteName;
        [DataField("startingInventory")] private Dictionary<string, uint> _startingInventory = new();

        public string ID => _id;
        public string Name => _name;
        public string Description => _description;
        public double AnimationDuration => _animationDuration;
        public string SpriteName => _spriteName;
        public Dictionary<string, uint> StartingInventory => _startingInventory;
    }
}
