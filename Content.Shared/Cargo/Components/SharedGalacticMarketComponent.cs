using System;
using System.Collections;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Cargo.Components
{
    [NetworkedComponent()]
    public class SharedGalacticMarketComponent : Component, IEnumerable<CargoProductPrototype>, ISerializationHooks
    {
        public sealed override string Name => "GalacticMarket";

        [DataField("products", customTypeSerializer: typeof(PrototypeIdListSerializer<CargoProductPrototype>))]
        protected List<string> _productIds = new();

        protected readonly List<CargoProductPrototype> _products = new();

        /// <summary>
        ///     A read-only list of products.
        /// </summary>
        public IReadOnlyList<CargoProductPrototype> Products => _products;

        void ISerializationHooks.AfterDeserialization()
        {
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

            _products.Clear();

            foreach (var id in _productIds)
            {
                if (!prototypeManager.TryIndex(id, out CargoProductPrototype? product))
                {
                    Logger.ErrorS("cargo", $"Unable to find {nameof(CargoProductPrototype)} for {id}");
                    continue;
                }

                _products.Add(product);
            }
        }

        void ISerializationHooks.BeforeSerialization()
        {
            _productIds.Clear();

            foreach (var product in _products)
            {
                _productIds.Add(product.ID);
            }
        }

        public IEnumerator<CargoProductPrototype> GetEnumerator()
        {
            return _products.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        ///     Returns a product from the string id;
        /// </summary>
        /// <returns>Product</returns>
        public CargoProductPrototype? GetProduct(string productId)
        {
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            if (!prototypeManager.TryIndex(productId, out CargoProductPrototype? product) || !_products.Contains(product))
            {
                return null;
            }
            return product;
        }

        /// <summary>
        ///     Returns a list with the IDs of all products.
        /// </summary>
        /// <returns>A list of product IDs</returns>
        public List<string> GetProductIdList()
        {
            var productIds = new List<string>();

            foreach (var product in _products)
            {
                productIds.Add(product.ID);
            }

            return productIds;
        }
    }

    [Serializable, NetSerializable]
    public class GalacticMarketState : ComponentState
    {
        public List<string> Products;
        public GalacticMarketState(List<string> technologies)
        {
            Products = technologies;
        }
    }
}
