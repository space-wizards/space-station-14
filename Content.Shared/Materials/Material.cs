#nullable enable
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Materials
{
    /// <summary>
    ///     Materials are read-only storage for the properties of specific materials.
    ///     Properties should be intrinsic (or at least as much is necessary for game purposes).
    /// </summary>
    [Prototype("material")]
    [DataDefinition]
    public class MaterialPrototype : IPrototype, IInheritingPrototype
    {
        [ViewVariables]
        [DataField("id", required: true)]
        public string ID { get; } = default!;

        [DataField("name")] public string Name { get; private set; } = "unobtanium";

        [DataField("color")] public Color Color { get; private set; } = Color.Gray;

        /// <summary>
        ///     Volumetric mass density, in kg.m^-3.
        /// </summary>
        [DataField("density")]
        public double Density { get; private set; } = 1;

        /// <summary>
        ///     Electrical resistivity, NOT resistance.
        ///     Unit is ohm-meter (Ω⋅m).
        /// </summary>
        [DataField("electricResistivity")]
        public double ElectricResistivity { get; private set; } = 1;

        /// <summary>
        ///     Thermal conductivity, in W.m-1.K-1
        /// </summary>
        [DataField("thermalConductivity")]
        public double ThermalConductivity { get; private set; } = 1;

        /// <summary>
        ///     Specific heat, in J.kg-1.K-1
        /// </summary>
        [DataField("specificHeat")]
        public double SpecificHeat { get; private set; } = 1;

        /// <summary>
        ///     Controls how durable the material is.
        ///     Basically how slowly it degrades.
        /// </summary>
        [DataField("durability")]
        public double Durability { get; private set; } = 1;

        /// <summary>
        ///     Multiplier for how much this resists damage.
        ///     So higher means armor is more effective, for example.
        /// </summary>
        [DataField("hardness")]
        public double Hardness { get; private set; } = 1;

        /// <summary>
        ///     Multiplier that determines damage on sharpness-based weapons like knives.
        ///     Higher means more damage is done.
        /// </summary>
        [DataField("sharpDamage")]
        public double SharpDamage { get; private set; } = 1;

        /// <summary>
        ///     Multiplier that determines damage on blunt-based weapons like clubs.
        ///     Higher means more damage is done.
        /// </summary>
        [DataField("bluntDamage")]
        public double BluntDamage { get; private set; } = 1;

        /// <summary>
        ///     An icon used to represent the material in graphic interfaces.
        /// </summary>
        [DataField("icon")]
        public SpriteSpecifier Icon { get; private set; } = SpriteSpecifier.Invalid;

        [DataField("parent")]
        public string? Parent { get; }

        [DataField("abstract")]
        public bool Abstract { get; }
    }
}
