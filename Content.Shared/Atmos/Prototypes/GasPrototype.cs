using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Atmos.Prototypes
{
    [Prototype("gas")]
    public sealed partial class GasPrototype : IPrototype
    {
        [DataField("name")] public string Name { get; set; } = "";

        // TODO: Control gas amount necessary for overlay to appear
        // TODO: Add interfaces for gas behaviours e.g. breathing, burning

        [ViewVariables]
        [IdDataField]
        public string ID { get; private set; } = default!;

        /// <summary>
        ///     Specific heat for gas.
        /// </summary>
        [DataField("specificHeat")]
        public float SpecificHeat { get; private set; }

        /// <summary>
        /// Heat capacity ratio for gas
        /// </summary>
        [DataField("heatCapacityRatio")]
        public float HeatCapacityRatio { get; private set; } = 1.4f;

        /// <summary>
        /// Molar mass of gas
        /// </summary>
        [DataField("molarMass")]
        public float MolarMass { get; set; } = 1f;


        /// <summary>
        ///     Minimum amount of moles for this gas to be visible.
        /// </summary>
        [DataField("gasMolesVisible")]
        public float GasMolesVisible { get; private set; } = 0.25f;

        /// <summary>
        ///     Visibility for this gas will be max after this value.
        /// </summary>
        public float GasMolesVisibleMax => GasMolesVisible * GasVisibilityFactor;

        [DataField("gasVisbilityFactor")]
        public float GasVisibilityFactor = Atmospherics.FactorGasVisibleMax;

        /// <summary>
        ///     If this reagent is in gas form, this is the path to the overlay that will be used to make the gas visible.
        /// </summary>
        [DataField("gasOverlayTexture")]
        public string GasOverlayTexture { get; private set; } = string.Empty;

        /// <summary>
        ///     If this reagent is in gas form, this will be the path to the RSI sprite that will be used to make the gas visible.
        /// </summary>
        [DataField("gasOverlayState")]
        public string GasOverlayState { get; set; } = string.Empty;

        /// <summary>
        ///     State for the gas RSI overlay.
        /// </summary>
        [DataField("gasOverlaySprite")]
        public string GasOverlaySprite { get; set; } = string.Empty;

        /// <summary>
        /// Path to the tile overlay used when this gas appears visible.
        /// </summary>
        [DataField("overlayPath")]
        public string OverlayPath { get; private set; } = string.Empty;

        /// <summary>
        /// The reagent that this gas will turn into when inhaled.
        /// </summary>
        [DataField("reagent", customTypeSerializer:typeof(PrototypeIdSerializer<ReagentPrototype>))]
        public string? Reagent { get; private set; } = default!;

        [DataField("color")] public string Color { get; private set; } = string.Empty;

        [DataField("pricePerMole")]
        public float PricePerMole { get; set; } = 0;
    }
}
