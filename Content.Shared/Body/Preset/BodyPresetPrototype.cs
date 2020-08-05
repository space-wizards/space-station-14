using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.GameObjects.Components.Body.Conduit;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Body.Preset
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
        private List<(string id, string part)> _mechanisms;
        private List<IBodyConduit> _conduits;

        [ViewVariables] public string ID => _id;
        
        [ViewVariables] public string Name => _name;

        [ViewVariables] public Dictionary<string, string> PartIDs => _partIDs.ToDictionary(x => x.Key, x => x.Value);

        [ViewVariables] public List<(string id, string part)> Mechanisms => _mechanisms.Select(x => (x.id, x.part)).ToList();

        [ViewVariables] public List<IBodyConduit> Conduits => _conduits.Select(c => c.Copy()).ToList();

        public virtual void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);
            
            serializer.DataField(ref _id, "id", string.Empty);
            serializer.DataField(ref _name, "name", string.Empty);
            serializer.DataField(ref _partIDs, "partIDs", new Dictionary<string, string>());

            var mechanisms = new List<(string id, string part)>();
            if (mapping.TryGetNode("mechanisms", out var node))
            {
                var sequence = (YamlSequenceNode) node;
                
                foreach (var yamlNode in sequence)
                {
                    var mechanism = (YamlMappingNode) yamlNode;
                    var id = mechanism["id"].AsString();
                    var part = mechanism["parts"].AsString(); // TODO: List?

                    mechanisms.Add((id, part));
                }
            }

            _mechanisms = mechanisms;
            
            _conduits = serializer.ReadDataField("conduits", new List<IBodyConduit>());
        }
    }
}
