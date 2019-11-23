using System;
using System.Collections.Generic;
using Content.Shared.Interfaces.Chemistry;
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
        private string _id;
        private string _name;
        private string _description;
        private Color _substanceColor;
        private List<IMetabolizable> _metabolism;

        public string ID => _id;
        public string Name => _name;
        public string Description => _description;
        public Color SubstanceColor => _substanceColor;
        //List of metabolism effects this reagent has, should really only be used server-side.
        public List<IMetabolizable> Metabolism => _metabolism;

        public void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);

            serializer.DataField(ref _id, "id", string.Empty);
            serializer.DataField(ref _name, "name", string.Empty);
            serializer.DataField(ref _description, "desc", string.Empty);
            serializer.DataField(ref _substanceColor, "color", Color.White);
            serializer.DataField(ref _metabolism, "metabolism", new List<IMetabolizable>{new DefaultMetabolizable()});
        }
    }
}
