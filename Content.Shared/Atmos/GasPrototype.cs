using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Atmos
{
    [Prototype("gas")]
    public class GasPrototype : IPrototype, IIndexedPrototype
    {
        public string Name { get; private set; }

        // TODO: Control gas amount necessary for overlay to appear
        // TODO: Add interfaces for gas behaviours e.g. breathing, burning

        public string ID { get; private set; }

        /// <summary>
        ///     Specific heat for gas.
        /// </summary>
        public float SpecificHeat { get; private set; }

        /// <summary>
        ///     Minimum amount of moles for this gas to be visible.
        /// </summary>
        public float GasMolesVisible { get; private set; }

        /// <summary>
        ///     Visibility for this gas will be max after this value.
        /// </summary>
        public float GasMolesVisibleMax => GasMolesVisible * Atmospherics.FactorGasVisibleMax;

        /// <summary>
        ///     If this reagent is in gas form, this is the path to the overlay that will be used to make the gas visible.
        /// </summary>
        public string GasOverlayTexture { get; private set; }

        /// <summary>
        ///     If this reagent is in gas form, this will be the path to the RSI sprite that will be used to make the gas visible.
        /// </summary>
        public string GasOverlayState { get; set; }

        /// <summary>
        ///     State for the gas RSI overlay.
        /// </summary>
        public string GasOverlaySprite { get; set; }

        /// <summary>
        /// Path to the tile overlay used when this gas appears visible.
        /// </summary>
        public string OverlayPath { get; private set; }

        public string Color { get; private set; }

        public void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);

            serializer.DataField(this, x => ID, "id", string.Empty);
            serializer.DataField(this, x => Name, "name", string.Empty);
            serializer.DataField(this, x => OverlayPath, "overlayPath", string.Empty);
            serializer.DataField(this, x => SpecificHeat, "specificHeat", 0f);
            serializer.DataField(this, x => GasMolesVisible, "gasMolesVisible", 0.25f);
            serializer.DataField(this, x => GasOverlayTexture, "gasOverlayTexture", string.Empty);
            serializer.DataField(this, x => GasOverlaySprite, "gasOverlaySprite", string.Empty);
            serializer.DataField(this, x => GasOverlayState, "gasOverlayState", string.Empty);
            serializer.DataField(this, x => Color, "color", string.Empty);
        }
    }
}
