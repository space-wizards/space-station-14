#nullable enable
using System;
using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.VendingMachines
{
    [Serializable, NetSerializable, Prototype("vendingMachineInventory")]
    public class VendingMachineInventoryPrototype : IPrototype
    {
        [ViewVariables]
        [DataField("id", required: true)]
        public string ID { get; } = default!;

        [DataField("name")]
        public string Name { get; } = string.Empty;

        [DataField("description")]
        public string Description { get; } = string.Empty;

        [DataField("animationDuration")]
        public double AnimationDuration { get; }

        [DataField("spriteName")]
        public string SpriteName { get; } = string.Empty;

        [DataField("startingInventory")]
        public Dictionary<string, uint> StartingInventory { get; } = new();
    }
}
