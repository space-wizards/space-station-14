using System.Linq;
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
                else if (SpawnList.Count > 0 && IoCManager.Resolve<IPrototypeManager>().Resolve(SpawnList.First(), out var prototype))
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
                else if (SpawnList.Count > 0 && IoCManager.Resolve<IPrototypeManager>().Resolve(SpawnList.First(), out var prototype))
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
        public EntProtoId? Product { get; private set; }

        /// <summary>
        ///     List of entity prototypes to spawn. If not set, falls back to <see cref="Product"/>.
        /// </summary>
        [DataField]
        public List<EntProtoId>? Products { get; private set; }

        /// <summary>
        ///     Resolved list of entities to spawn. Always use this instead of <see cref="Product"/> directly.
        /// </summary>
        public List<EntProtoId> SpawnList
        {
            get
            {
                if (Products != null)
                    return Products;
                if (Product != null)
                    return new List<EntProtoId> { Product.Value };
                throw new InvalidOperationException($"CargoProductPrototype {ID} has neither Product nor Products defined.");
            }
        }


        /// <summary>
        /// The prototype of the container to be spawned
        /// </summary>
        // This prototype might be redundent as the capacity and access can porbably be gotten from the entity Id.
        // For livestock crates and setting a lower max items than the capaicty this is currently useful
        [DataField]
        public ProtoId<CargoCratePrototype>? Container;

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

    [Prototype]
    public sealed partial class CargoCratePrototype : IPrototype
    {
        /// <summary>
        /// ID of prototype.
        /// </summary>
        [ViewVariables]
        [IdDataField]
        public string ID { get; private set; } = default!;

        [DataField(required: true)]
        /// <summary>
        /// What entity to spawn as the container.
        /// </summary>
        public EntProtoId<ContainerManagerComponent> Entity;
        [DataField(required: true)]
        /// <summary>
        /// Component for spawning entities.
        /// </summary>
        public string ContainerId = string.Empty;
        [DataField(required: true)]
        /// <summary>
        /// Max amount of items that can spawn in a container.
        /// </summary>
        public int MaxItems;
        [DataField]
        /// <summary>
        /// Whether or not this container is required for the item to spawn into.
        /// </summary>
        public bool Required = false;
        [DataField]
        /// <summary>
        /// Whether or not this container is required for the item to spawn into.
        /// </summary>
        public int Cost;
    }
}
