using System.Collections.Generic;
using Content.Shared.Prototypes.Cargo;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.GameObjects.Components.Cargo
{
    public partial class SharedGalacticMarketDataClass : ISerializationHooks
    {
        [DataField("products")]
        protected List<string> _productIds = new();

        [DataClassTarget("productsTarget")]
        protected List<CargoProductPrototype> _products = new();

        public void AfterDeserialization()
        {
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

            _products.Clear();
            foreach (var id in _productIds)
            {
                if (!prototypeManager.TryIndex(id, out CargoProductPrototype product))
                {
                    continue;
                }

                _products.Add(product);
            }
        }

        public void BeforeSerialization()
        {
            _productIds = new List<string>();

            foreach (var product in _products)
            {
                _productIds.Add(product.ID);
            }
        }
    }
}
