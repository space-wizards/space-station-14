using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;

namespace Content.Shared.Cargo.Prototypes
{
    [NetSerializable, Serializable, Prototype("cargoProduct")]
    public readonly record struct CargoProductPrototype : IPrototype
    {
        [DataField("name")] private readonly string _name = string.Empty;

        [DataField("description")] private readonly string _description = string.Empty;

        [ViewVariables]
        [IdDataField]
        public string ID { get; } = default!;

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

                return IoCManager.Resolve<IPrototypeManager>().TryIndex(Product, out EntityPrototype? prototype)
                    ? prototype.Value.Name
                    : _name;
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
                if (!string.IsNullOrWhiteSpace(_description))
                    return _description;

                return IoCManager.Resolve<IPrototypeManager>().TryIndex(Product, out EntityPrototype? prototype)
                    ? prototype.Value.Description
                    : _description;
            }
        }

        /// <summary>
        ///     Texture path used in the CargoConsole GUI.
        /// </summary>
        [ViewVariables]
        [DataField("icon")]
        public SpriteSpecifier Icon { get; } = SpriteSpecifier.Invalid;

        /// <summary>
        ///     The prototype name of the product.
        /// </summary>
        [ViewVariables]
        [DataField("product", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string Product { get; } = string.Empty;

        /// <summary>
        ///     The point cost of the product.
        /// </summary>
        [ViewVariables]
        [DataField("cost")]
        public int PointCost { get; }

        /// <summary>
        ///     The prototype category of the product. (e.g. Engineering, Medical)
        /// </summary>
        [ViewVariables]
        [DataField("category")]
        public string Category { get; } = string.Empty;

        /// <summary>
        ///     The prototype group of the product. (e.g. Contraband)
        /// </summary>
        [ViewVariables]
        [DataField("group")]
        public string Group { get; } = string.Empty;
    }
}
