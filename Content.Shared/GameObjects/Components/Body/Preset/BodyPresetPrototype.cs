using System;
using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.GameObjects.Components.Body.Preset
{
    /// <summary>
    ///     Prototype for the BodyPreset class.
    /// </summary>
    [Prototype("bodyPreset")]
    [Serializable, NetSerializable]
    public class BodyPresetPrototype : IPrototype, IIndexedPrototype
    {
        private string _id;
        private string _name;
        private Dictionary<string, string> _partIDs;

        [ViewVariables] public string ID => _id;

        [ViewVariables] public string Name => _name;

        [ViewVariables] public Dictionary<string, string> PartIDs => new Dictionary<string, string>(_partIDs);

        public virtual void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);
            serializer.DataField(ref _id, "id", string.Empty);
            serializer.DataField(ref _name, "name", string.Empty);
            serializer.DataField(ref _partIDs, "partIDs", new Dictionary<string, string>());
        }
    }
}
