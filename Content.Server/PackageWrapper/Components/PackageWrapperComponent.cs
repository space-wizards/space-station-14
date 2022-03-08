using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Content.Server.PackageWrapper.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.PackageWrapper
{
    [RegisterComponent]
    public class PackageWrapperComponent : Component, IEnumerable<WrapperTypePrototype>, ISerializationHooks
    {
        [Dependency] private readonly IEntityManager _entMan = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public sealed override string Name => "PackageWrapper";

        protected List<WrapperTypePrototype> _products = new();
        protected List<WrapperShapedTypePrototype> _productsShaped = new();

        /// <summary>
        ///     A read-only list of wraps.
        /// </summary>
        public IReadOnlyList<WrapperTypePrototype> Products => _products;
        public IReadOnlyList<WrapperShapedTypePrototype> ProductsShaped => _productsShaped;

        protected override void Initialize()
        {
            base.Initialize();

            //Charges = Capacity; // make here capacity init

            // I dont know how to get list of all my warpType prototypes so i use that.
            // I'm really need to know how to init data once for all PackageWrappers
            _products = _prototypeManager.EnumeratePrototypes<WrapperTypePrototype>().ToList();
            _productsShaped = _prototypeManager.EnumeratePrototypes<WrapperShapedTypePrototype>().ToList();

            Dirty();
        }

        void ISerializationHooks.AfterDeserialization()
        {

        }

        void ISerializationHooks.BeforeSerialization()
        {

        }

        public IEnumerator<WrapperTypePrototype> GetEnumerator()
        {
            return _products.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        ///     Returns a WrapContainer from the string id;
        /// </summary>
        /// <returns>Product</returns>
        public WrapperTypePrototype? GetProduct(string productId)
        {
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            if (!prototypeManager.TryIndex(productId, out WrapperTypePrototype? product) || !_products.Contains(product))
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
}
