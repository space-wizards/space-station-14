using System;
using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Jobs
{
    [Prototype("startingGear")]
    public class StartingGearPrototype : IPrototype, IIndexedPrototype
    {
        private string _id;
        private Dictionary<string, string> _equipment;

        [ViewVariables]
        public string ID => _id;

        [ViewVariables]
        public Dictionary<string, string> Equipment => _equipment;

        public void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);

            serializer.DataField(ref _id, "id", string.Empty);
            serializer.DataField(ref _equipment, "equipment", new Dictionary<string, string>());
        }
    }
}
