using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Utility;

namespace Content.Shared.Cargo.Prototypes
{
    [Prototype]
    public sealed partial class CargoProductPrototype : IPrototype, IInheritingPrototype
    {
        /// <inheritdoc />
        [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<CargoProductPrototype>))]
        public string[]? Parents { get; private set; }

        /// <inheritdoc />
        [NeverPushInheritance]
        [AbstractDataField]
        public bool Abstract { get; private set; }

        [DataField("name")]
        private LocId? _nameLoc;

        private string _name = string.Empty;

        [DataField("description")]
        private LocId? _descLoc;

        private string _description = string.Empty;

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

                if (_nameLoc is { } nameLoc)
                {
                    _name = Loc.GetString(nameLoc);
                }
                else if (IoCManager.Resolve<IPrototypeManager>().Resolve(Product, out var prototype))
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

                if (_descLoc is { } descLoc)
                {
                    _description = Loc.GetString(descLoc);
                }
                else if (IoCManager.Resolve<IPrototypeManager>().Resolve(Product, out var prototype))
                {
                    _description = prototype.Description;
                }

                return _description;
            }
        }

        /// <summary>
        ///     Texture path used in the CargoConsole GUI.
        /// </summary>
        [DataField]
        public SpriteSpecifier Icon { get; private set; } = SpriteSpecifier.Invalid;

        /// <summary>
        ///     The entity prototype ID of the product.
        /// </summary>
        [DataField]
        public EntProtoId Product { get; private set; } = string.Empty;

        /// <summary>
        /// The entity to spawn and insert the product into. If null, just the product is spawned.
        /// </summary>
        [DataField]
        public CargoProductContainer? Container;

        /// <summary>
        ///     The point cost of the product.
        /// </summary>
        [DataField]
        public int Cost { get; private set; }

        /// <summary>
        ///     The prototype category of the product. (e.g. Engineering, Medical)
        /// </summary>
        [DataField]
        public string Category { get; private set; } = string.Empty;

        /// <summary>
        ///     The prototype group of the product. (e.g. Contraband)
        /// </summary>
        [DataField]
        public ProtoId<CargoMarketPrototype> Group { get; private set; } = "market";
    }

    /// <see cref="CargoProductPrototype.Container"/>
    [DataDefinition, Serializable, NetSerializable]
    public sealed partial class CargoProductContainer
    {
        /// <summary>
        /// What entity to spawn as the container.
        /// </summary>
        [DataField(required: true)]
        public EntProtoId<ContainerManagerComponent> Entity;

        /// <summary>
        /// What container in <see cref="Entity"/> the product should be inserted into.
        /// </summary>
        [DataField(required: true)]
        public string ContainerId;
    }
}
