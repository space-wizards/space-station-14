using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System;
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

        public float SpecificHeat { get; private set; }

        /// <summary>
        /// Path to the tile overlay used when this gas appears visible.
        /// </summary>
        public string OverlayPath { get; private set; }

        public void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);

            serializer.DataField(this, x => ID, "id", string.Empty);
            serializer.DataField(this, x => Name, "name", string.Empty);
            serializer.DataField(this, x => OverlayPath, "overlayPath", string.Empty);
            serializer.DataField(this, x => SpecificHeat, "specificHeat", 0f);
        }
    }
}
