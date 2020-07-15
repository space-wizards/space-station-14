using System.Collections.Generic;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.Chemistry;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Chemistry
{
    [Prototype("reagent")]
    public class ReagentPrototype : IPrototype, IIndexedPrototype
    {
        private const float CelsiusToKelvin = 273.15f;

#pragma warning disable 649
        [Dependency] private readonly IModuleManager _moduleManager;
#pragma warning restore 649

        private string _id;
        private string _name;
        private string _description;
        private Color _substanceColor;
        private List<IMetabolizable> _metabolism;
        private string _spritePath;

        public string ID => _id;
        public string Name => _name;
        public string Description => _description;
        public Color SubstanceColor => _substanceColor;
        //List of metabolism effects this reagent has, should really only be used server-side.
        public List<IMetabolizable> Metabolism => _metabolism;
        public string SpriteReplacementPath => _spritePath;

        /// <summary>
        ///     Specific heat for gas.
        /// </summary>
        public float SpecificHeat { get; private set; }

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
        ///     Sprite specifier for the gas overlay.
        /// </summary>
        public SpriteSpecifier GasOverlay
        {
            get
            {
                if(string.IsNullOrEmpty(GasOverlaySprite) && !string.IsNullOrEmpty(GasOverlayTexture))
                    return new SpriteSpecifier.Texture(new ResourcePath(GasOverlayTexture));

                if(!string.IsNullOrEmpty(GasOverlaySprite) && !string.IsNullOrEmpty(GasOverlayState))
                    return new SpriteSpecifier.Rsi(new ResourcePath(GasOverlaySprite), GasOverlayState);

                return null;
            }
        }

        /// <summary>
        ///     Boiling point in Cº for this chemical.
        /// </summary>
        public float BoilingPoint { get; private set; }

        /// <summary>
        ///     Melting point in Cº for this chemical.
        /// </summary>
        public float MeltingPoint { get; private set; }

        /// <summary>
        ///     Boiling point in Kº for this chemical.
        /// </summary>
        public float BoilingPointKelvin => BoilingPoint + CelsiusToKelvin;

        /// <summary>
        ///     Melting point in Kº for this chemical.
        /// </summary>
        public float MeltingPointKelvin => MeltingPoint + CelsiusToKelvin;

        public ReagentPrototype()
        {
            IoCManager.InjectDependencies(this);
        }

        public void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);

            serializer.DataField(ref _id, "id", string.Empty);
            serializer.DataField(ref _name, "name", string.Empty);
            serializer.DataField(ref _description, "desc", string.Empty);
            serializer.DataField(ref _substanceColor, "color", Color.White);
            serializer.DataField(ref _spritePath, "spritePath", string.Empty);
            serializer.DataField(this, x => BoilingPoint, "boilingPoint", 0);
            serializer.DataField(this, x => MeltingPoint, "meltingPoint", 0);
            serializer.DataField(this, x => GasOverlayTexture, "gasOverlayTexture", string.Empty);
            serializer.DataField(this, x => GasOverlaySprite, "gasOverlaySprite", string.Empty);
            serializer.DataField(this, x => GasOverlayState, "gasOverlayState", string.Empty);
            serializer.DataField(this, x => SpecificHeat, "specificHeat", 0);

            if (_moduleManager.IsServerModule)
                serializer.DataField(ref _metabolism, "metabolism", new List<IMetabolizable> {new DefaultMetabolizable()});
            else
                _metabolism = new List<IMetabolizable> { new DefaultMetabolizable() };
        }
    }
}
