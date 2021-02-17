using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Prototypes.Cargo
{
    [NetSerializable, Serializable, Prototype("cargoProduct")]
    public class CargoProductPrototype : IPrototype, IIndexedPrototype
    {
        [YamlField("id")]
        private string _id;
        [YamlField("name")]
        private string _name;
        [YamlField("description")]
        private string _description;
        [YamlField("icon")]
        private SpriteSpecifier _icon;
        [YamlField("product")]
        private string _product;
        [YamlField("cost")]
        private int _pointCost;
        [YamlField("category")]
        private string _category;
        [YamlField("group")]
        private string _group;

        [ViewVariables]
        public string ID => _id;

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
                EntityPrototype prototype = null;
                IoCManager.Resolve<IPrototypeManager>()?.TryIndex(_product, out prototype);
                if (prototype?.Name != null)
                    _name = prototype.Name;
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
                EntityPrototype prototype = null;
                IoCManager.Resolve<IPrototypeManager>()?.TryIndex(_product, out prototype);
                if (prototype?.Description != null)
                    _description = prototype.Description;
                return _description;
            }
        }

        /// <summary>
        ///     Texture path used in the CargoConsole GUI.
        /// </summary>
        [ViewVariables]
        public SpriteSpecifier Icon => _icon;

        /// <summary>
        ///     The prototype name of the product.
        /// </summary>
        [ViewVariables]
        public string Product => _product;

        /// <summary>
        ///     The point cost of the product.
        /// </summary>
        [ViewVariables]
        public int PointCost => _pointCost;

        /// <summary>
        ///     The prototype category of the product. (e.g. Engineering, Medical)
        /// </summary>
        [ViewVariables]
        public string Category => _category;

        /// <summary>
        ///     The prototype group of the product. (e.g. Contraband)
        /// </summary>
        [ViewVariables]
        public string Group => _group;

        public CargoProductPrototype()
        {
            IoCManager.InjectDependencies(this);
        }
    }
}
