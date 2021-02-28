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
        [field: DataField("id", required: true)]
        public string ID { get; } = default!;

        [ViewVariables]
        [field: DataField("parent")]
        public string? Parent { get; }

        [field: DataField("name")]
        public string Name { get; } = string.Empty;

        [field: DataField("description")]
        public string Description { get; } = string.Empty;

        [field: DataField("animationDuration")]
        public double AnimationDuration { get; }

        [field: DataField("spriteName")]
        public string SpriteName { get; } = string.Empty;

        [field: DataField("startingInventory")]
        public Dictionary<string, uint> StartingInventory { get; } = new();
    }
}
