#nullable enable
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Atmos
{
    [Prototype("gas")]
    public class GasPrototype : IPrototype
    {
        [DataField("name")] public string Name { get; } = string.Empty;

        // TODO: Control gas amount necessary for overlay to appear
        // TODO: Add interfaces for gas behaviours e.g. breathing, burning

        [ViewVariables]
        [DataField("id", required: true)]
        public string ID { get; } = default!;

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
        public float GasMolesVisible { get; } = 0.25f;

        /// <summary>
        ///     Visibility for this gas will be max after this value.
        /// </summary>
        public float GasMolesVisibleMax => GasMolesVisible * Atmospherics.FactorGasVisibleMax;

        /// <summary>
        ///     If this reagent is in gas form, this is the path to the overlay that will be used to make the gas visible.
        /// </summary>
        [DataField("gasOverlayTexture")]
        public string GasOverlayTexture { get; } = string.Empty;

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
        public string OverlayPath { get; } = string.Empty;

        [DataField("color")] public string Color { get; } = string.Empty;
    }
}
