#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using Content.Shared.Prototypes.Cargo;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Cargo
{
    public class SharedGalacticMarketComponent : Component, IEnumerable<CargoProductPrototype>
    {
        public sealed override string Name => "GalacticMarket";
        public sealed override uint? NetID => ContentNetIDs.GALACTIC_MARKET;

        protected List<CargoProductPrototype> _products = new();

        /// <summary>
        ///     A read-only list of products.
        /// </summary>
        public IReadOnlyList<CargoProductPrototype> Products => _products;

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
            List<string> productIds = new List<string>();

            foreach (var product in _products)
            {
                productIds.Add(product.ID);
            }

            return productIds;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataReadWriteFunction(
                "products",
                new List<string>(),
                products =>
                {
                    var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

                    _products.Clear();
                    foreach (var id in products)
                    {
                        if (!prototypeManager.TryIndex(id, out CargoProductPrototype? product))
                        {
                            continue;
                        }

                        _products.Add(product);
                    }
                },
                GetProductIdList);
        }
    }

    [Serializable, NetSerializable]
    public class GalacticMarketState : ComponentState
    {
        public List<string> Products;
        public GalacticMarketState(List<string> technologies) : base(ContentNetIDs.GALACTIC_MARKET)
        {
            Products = technologies;
        }
    }
}
