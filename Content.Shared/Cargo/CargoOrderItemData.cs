using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Cargo
{
    [DataDefinition, NetSerializable, Serializable]
    public sealed partial record class CargoOrderItemData
    {
        /// <summary>
        /// The ID of the cargo product ordered.
        /// </summary>
        [DataField]
        public ProtoId<CargoProductPrototype> Product;

        /// <summary>
        /// The number of products ordered.
        /// </summary>
        [DataField]
        public int Quantity;

        /// <summary>
        /// Whether or not this item should spawn with a container.
        /// </summary>
        // Currently unused
        [DataField]
        public bool WithContainer = true;

        /// <summary>
        /// Whether or not this item should be ordered with the rest of the basket.
        /// </summary>
        // Currently unused
        public bool ToBeOrdered = true;

        /// <summary>
        /// Tracks the number of spawned items out of the total quantity.
        /// </summary>
        [DataField]
        public int NumOrdered = 0;

        public CargoOrderItemData(ProtoId<CargoProductPrototype> product, int quantity)
        {
            Product = product;
            Quantity = quantity;
        }
    }
}
