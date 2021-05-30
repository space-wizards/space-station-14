#nullable enable
using System;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Prototypes.Cargo
{
    [NetSerializable, Serializable, Prototype("cargoProduct")]
    public class CargoProductPrototype : IPrototype
    {
        [DataField("name")] private string _name = string.Empty;

        [DataField("description")] private string _description = string.Empty;

        [ViewVariables]
        [DataField("id", required: true)]
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
        [ViewVariables]
        [DataField("icon")]
        public SpriteSpecifier Icon { get; } = SpriteSpecifier.Invalid;

        /// <summary>
        ///     The prototype name of the product.
        /// </summary>
        [ViewVariables]
        [DataField("product")]
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
