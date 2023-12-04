using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;

namespace Content.Shared.Cargo.Prototypes
{
    [Prototype("cargoProduct")]
    public sealed partial class CargoProductPrototype : IPrototype
    {
        [DataField("name")] private string _name = string.Empty;

        [DataField("description")] private string _description = string.Empty;

        [ViewVariables]
        [IdDataField]
        public string ID { get; private set; } = default!;

        /// <summary>
        ///     Product name.
        /// </summary>
        [ViewVariables]
        public string Name
        {
            get
            {
                if (_name.Trim().Length != 0)
                    return _name;

                if (IoCManager.Resolve<IPrototypeManager>().TryIndex(Product, out EntityPrototype? prototype))
                {
                    _name = prototype.Name;
                }

                return _name;
            }
        }

        /// <summary>
        ///     Short description of the product.
        /// </summary>
        [ViewVariables]
        public string Description
        {
            get
            {
                if (_description.Trim().Length != 0)
                    return _description;

                if (IoCManager.Resolve<IPrototypeManager>().TryIndex(Product, out EntityPrototype? prototype))
                {
                    _description = prototype.Description;
                }

                return _description;
            }
        }

        /// <summary>
        ///     Texture path used in the CargoConsole GUI.
        /// </summary>
        [DataField("icon")]
        public SpriteSpecifier Icon { get; private set; } = SpriteSpecifier.Invalid;

        /// <summary>
        ///     The prototype name of the product.
        /// </summary>
        [DataField("product", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string Product { get; private set; } = string.Empty;

        /// <summary>
        ///     The point cost of the product.
        /// </summary>
        [DataField("cost")]
        public int PointCost { get; private set; }

        /// <summary>
        ///     The prototype category of the product. (e.g. Engineering, Medical)
        /// </summary>
        [DataField("category")]
        public string Category { get; private set; } = string.Empty;

        /// <summary>
        ///     The prototype group of the product. (e.g. Contraband)
        /// </summary>
        [DataField("group")]
        public string Group { get; private set; } = string.Empty;
    }
}
