using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Text;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.GameObjects.Components
{
    [NetSerializable]
    [Serializable]
    public enum CrayonVisuals
    {
        State,
        Color
    }

    [Serializable, NetSerializable, Prototype("crayonDecal")]
    public class CrayonDecalPrototype : IPrototype
    {
        private List<string> _decals;
        public List<string> Decals => _decals;

        public void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);

            serializer.DataField(ref _decals, "decals", new List<string>());
        }
    }
}
