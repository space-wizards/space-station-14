using System;
using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Body.BodyPreset
{
    /// <summary>
    ///     Prototype for the BodyPreset class.
    /// </summary>
    [Prototype("bodyPreset")]
    [NetSerializable]
    [Serializable]
    public class BodyPresetPrototype : IPrototype, IIndexedPrototype
    {
        private string _id;
        private string _name;
        private Dictionary<string, string> _partIDs;

        [ViewVariables] public string Name => _name;

        [ViewVariables] public Dictionary<string, string> PartIDs => _partIDs;

        [ViewVariables] public string ID => _id;

        public virtual void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);
            serializer.DataField(ref _id, "id", string.Empty);
            serializer.DataField(ref _name, "name", string.Empty);
            serializer.DataField(ref _partIDs, "partIDs", new Dictionary<string, string>());
        }
    }
}
