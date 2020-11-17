using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.GameObjects.Components.Body.Part;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.GameObjects.Components.Body.Template
{
    /// <summary>
    ///     Defines the layout of a <see cref="IBody"/>.
    /// </summary>
    [Prototype("bodyTemplate")]
    [Serializable, NetSerializable]
    public class BodyTemplatePrototype : IPrototype, IIndexedPrototype
    {
        private string _id;
        private string _name;
        private string _centerSlot;
        private Dictionary<string, BodyPartType> _slots;
        private Dictionary<string, List<string>> _connections;
        private Dictionary<string, string> _layers;
        private Dictionary<string, string> _mechanismLayers;

        [ViewVariables] public string ID => _id;

        [ViewVariables] public string Name => _name;

        [ViewVariables] public string CenterSlot => _centerSlot;

        [ViewVariables] public Dictionary<string, BodyPartType> Slots => new Dictionary<string, BodyPartType>(_slots);

        [ViewVariables]
        public Dictionary<string, List<string>> Connections =>
            _connections.ToDictionary(x => x.Key, x => x.Value.ToList());

        [ViewVariables] public Dictionary<string, string> Layers => new Dictionary<string, string>(_layers);

        [ViewVariables] public Dictionary<string, string> MechanismLayers => new Dictionary<string, string>(_mechanismLayers);

        public virtual void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);
            serializer.DataField(ref _id, "id", string.Empty);
            serializer.DataField(ref _name, "name", string.Empty);
            serializer.DataField(ref _centerSlot, "centerSlot", string.Empty);
            serializer.DataField(ref _slots, "slots", new Dictionary<string, BodyPartType>());
            serializer.DataField(ref _connections, "connections", new Dictionary<string, List<string>>());
            serializer.DataField(ref _layers, "layers", new Dictionary<string, string>());
            serializer.DataField(ref _mechanismLayers, "mechanismLayers", new Dictionary<string, string>());

            //Our prototypes don't force the user to define a BodyPart connection twice. E.g. Head: Torso v.s. Torso: Head.
            //The user only has to do one. We want it to be that way in the code, though, so this cleans that up.
            var cleanedConnections = new Dictionary<string, List<string>>();
            foreach (var targetSlotName in _slots.Keys)
            {
                var tempConnections = new List<string>();
                foreach (var (slotName, slotConnections) in _connections)
                {
                    if (slotName == targetSlotName)
                    {
                        foreach (var connection in slotConnections)
                        {
                            if (!tempConnections.Contains(connection))
                            {
                                tempConnections.Add(connection);
                            }
                        }
                    }
                    else if (slotConnections.Contains(targetSlotName))
                    {
                        tempConnections.Add(slotName);
                    }
                }

                if (tempConnections.Count > 0)
                {
                    cleanedConnections.Add(targetSlotName, tempConnections);
                }
            }

            _connections = cleanedConnections;
        }
    }
}
