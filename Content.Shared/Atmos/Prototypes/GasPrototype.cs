using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Atmos.Prototypes
{
    [Prototype("gas")]
    public readonly record struct GasPrototype : IPrototype
    {
        [DataField("name", customTypeSerializer: typeof(LocStringSerializer))]
        public readonly string Name = string.Empty;

        // TODO: Control gas amount necessary for overlay to appear
        // TODO: Add interfaces for gas behaviours e.g. breathing, burning

        [ViewVariables]
        [IdDataFieldAttribute]
        public string ID { get; } = default!;

        /// <summary>
        ///     Specific heat for gas.
        /// </summary>
        [DataField("specificHeat")]
        public float SpecificHeat { get; }

        /// <summary>
        /// Heat capacity ratio for gas
        /// </summary>
        [DataField("heatCapacityRatio")]
        public float HeatCapacityRatio { get; } = 1.4f;

        /// <summary>
        /// Molar mass of gas
        /// </summary>
        [DataField("molarMass")]
        public float MolarMass { get; } = 1f;


        /// <summary>
        ///     Minimum amount of moles for this gas to be visible.
        /// </summary>
        [DataField("gasMolesVisible")]
        public float GasMolesVisible { get; } = 0.25f;

        /// <summary>
        ///     Visibility for this gas will be max after this value.
        /// </summary>
        public float GasMolesVisibleMax => GasMolesVisible * GasVisibilityFactor;

        [DataField("gasVisbilityFactor")] public readonly float GasVisibilityFactor = Atmospherics.FactorGasVisibleMax;

        /// <summary>
        ///     If this reagent is in gas form, this is the path to the overlay that will be used to make the gas visible.
        /// </summary>
        [DataField("gasOverlayTexture")]
        public string GasOverlayTexture { get; } = string.Empty;

        /// <summary>
        ///     If this reagent is in gas form, this will be the path to the RSI sprite that will be used to make the gas visible.
        /// </summary>
        [DataField("gasOverlayState")]
        public string GasOverlayState { get; } = string.Empty;

        /// <summary>
        ///     State for the gas RSI overlay.
        /// </summary>
        [DataField("gasOverlaySprite")]
        public string GasOverlaySprite { get; } = string.Empty;

        /// <summary>
        /// Path to the tile overlay used when this gas appears visible.
        /// </summary>
        [DataField("overlayPath")]
        public string OverlayPath { get; } = string.Empty;

        /// <summary>
        /// The reagent that this gas will turn into when inhaled.
        /// </summary>
        [DataField("reagent", customTypeSerializer:typeof(PrototypeIdSerializer<ReagentPrototype>))]
        public string? Reagent { get; } = default!;

        [DataField("color")] public string Color { get; } = string.Empty;

        [DataField("pricePerMole")] public readonly float PricePerMole { get; }
    }
}
