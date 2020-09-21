using System;
using System.Collections.Generic;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.Chemistry;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Chemistry
{
    [Prototype("reagent")]
    public class ReagentPrototype : IPrototype, IIndexedPrototype
    {
        private const float CelsiusToKelvin = 273.15f;

        [Dependency] private readonly IModuleManager _moduleManager = default!;

        private string _id;
        private string _name;
        private string _description;
        private string _physicalDescription;
        private Color _substanceColor;
        private List<IMetabolizable> _metabolism;
        private string _spritePath;

        public string ID => _id;
        public string Name => _name;
        public string Description => _description;
        public string PhysicalDescription => _physicalDescription;
        public Color SubstanceColor => _substanceColor;

        //List of metabolism effects this reagent has, should really only be used server-side.
        public List<IMetabolizable> Metabolism => _metabolism;
        public string SpriteReplacementPath => _spritePath;

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
            serializer.DataField(ref _physicalDescription, "physicalDesc", string.Empty);
            serializer.DataField(ref _substanceColor, "color", Color.White);
            serializer.DataField(ref _spritePath, "spritePath", string.Empty);

            if (_moduleManager.IsServerModule)
            {
                serializer.DataField(ref _metabolism, "metabolism", new List<IMetabolizable> { new DefaultMetabolizable() });
            }
            else
            {
                _metabolism = new List<IMetabolizable> { new DefaultMetabolizable() };
            }
        }

        /// <summary>
        /// If the substance color is too dark we user a lighter version to make the text color readable when the user examines a solution.
        /// </summary>
        public Color GetSubstanceTextColor()
        {
            var highestValue = MathF.Max(SubstanceColor.R, MathF.Max(SubstanceColor.G, SubstanceColor.B));
            var difference = 0.5f - highestValue;

            if (difference > 0f)
            {
                return new Color(SubstanceColor.R + difference,
                                SubstanceColor.G + difference,
                                SubstanceColor.B + difference);
            }

            return SubstanceColor;
        }
    }
}
