using System.Collections.Generic;
using Content.Shared.Prototypes.Cargo;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Cargo
{
    public partial class SharedGalacticMarketDataClass
    {
        [DataClassTarget("products")]
        protected List<CargoProductPrototype> _products = new();

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
                        if (!prototypeManager.TryIndex(id, out CargoProductPrototype product))
                        {
                            continue;
                        }

                        _products.Add(product);
                    }
                },
                () =>
                {
                    var productIds = new List<string>();

                    foreach (var product in _products)
                    {
                        productIds.Add(product.ID);
                    }

                    return productIds;
                });
        }
    }
}
