using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Materials
{
    /// <summary>
    ///     Materials are read-only storage for the properties of specific materials.
    ///     Properties should be intrinsic (or at least as much is necessary for game purposes).
    /// </summary>
    public class Material : IExposeData
    {
        public string Name => _name;
        private string _name = "unobtanium";

        public Color Color => _color;
        private Color _color = Color.Gray;

        /// <summary>
        ///     Volumetric mass density, in kg.m^-3.
        /// </summary>
        public double Density => _density;
        private double _density = 1;

        /// <summary>
        ///     Electrical resistivity, NOT resistance.
        ///     Unit is ohm-meter (Ω⋅m).
        /// </summary>
        public double ElectricResistivity => _electricResistivity;
        private double _electricResistivity = 1;

        /// <summary>
        ///     Thermal conductivity, in W.m-1.K-1
        /// </summary>
        public double ThermalConductivity => _thermalConductivity;
        private double _thermalConductivity = 1;

        /// <summary>
        ///     Specific heat, in J.kg-1.K-1
        /// </summary>
        public double SpecificHeat => _specificHeat;
        private double _specificHeat = 1;

        /// <summary>
        ///     Controls how durable the material is.
        ///     Basically how slowly it degrades.
        /// </summary>
        public double Durability => _durability;
        private double _durability = 1;

        /// <summary>
        ///     Multiplier for how much this resists damage.
        ///     So higher means armor is more effective, for example.
        /// </summary>
        public double Hardness => _hardness;
        private double _hardness = 1;

        /// <summary>
        ///     Multiplier that determines damage on sharpness-based weapons like knives.
        ///     Higher means more damage is done.
        /// </summary>
        public double SharpDamage => _sharpDamage;
        private double _sharpDamage = 1;

        /// <summary>
        ///     Multiplier that determines damage on blunt-based weapons like clubs.
        ///     Higher means more damage is done.
        /// </summary>
        public double BluntDamage => _bluntDamage;
        private double _bluntDamage = 1;

        /// <summary>
        ///     An icon used to represent the material in graphic interfaces.
        /// </summary>
        public SpriteSpecifier Icon => _icon;
        private SpriteSpecifier _icon = SpriteSpecifier.Invalid;

        public string ID
        {
            get
            {
                var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
                foreach (var prototype in prototypeManager.EnumeratePrototypes<MaterialPrototype>())
                {
                    if (prototype.Material == this) return prototype.ID;
                }

                return null;
            }
        }

        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _name, "name", "unobtanium", alwaysWrite: true);
            serializer.DataField(ref _color, "color", Color.Gray, alwaysWrite: true);

            // All default material params are initialized to 1 because
            // I'm too lazy to figure out for which that's necessary to prevent divisions by zero in case left out.
            serializer.DataField(ref _density, "density", 1, alwaysWrite: true);
            serializer.DataField(ref _electricResistivity, "electricResistivity", 1, alwaysWrite: true);
            serializer.DataField(ref _thermalConductivity, "thermalConductivity", 1, alwaysWrite: true);
            serializer.DataField(ref _specificHeat, "specificHeat", 1, alwaysWrite: true);
            serializer.DataField(ref _durability, "durability", 1, alwaysWrite: true);
            serializer.DataField(ref _hardness, "hardness", 1, alwaysWrite: true);
            serializer.DataField(ref _sharpDamage, "sharpDamage", 1, alwaysWrite: true);
            serializer.DataField(ref _bluntDamage, "bluntDamage", 1, alwaysWrite: true);
            serializer.DataField(ref _icon, "icon", SpriteSpecifier.Invalid, alwaysWrite: true);
        }
    }

    [Prototype("material")]
    public class MaterialPrototype : IPrototype, IIndexedPrototype
    {
        public string ID { get; private set; }

        public Material Material { get; private set; }

        public void LoadFrom(YamlMappingNode mapping)
        {
            ID = mapping["id"].AsString();

            var ser = YamlObjectSerializer.NewReader(mapping);
            Material = new Material();
            ((IExposeData) Material).ExposeData(ser);
        }
    }
}
