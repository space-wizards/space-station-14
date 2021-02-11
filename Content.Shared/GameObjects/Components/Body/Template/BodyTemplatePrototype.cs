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
        [YamlField("id")]
        private string _id;
        [YamlField("name")]
        private string _name;
        [YamlField("centerSlot")]
        private string _centerSlot;
        [YamlField("slots")]
        private Dictionary<string, BodyPartType> _slots;
        private Dictionary<string, List<string>> _connections;
        [YamlField("layers")]
        private Dictionary<string, string> _layers;
        [YamlField("mechanismLayers")]
        private Dictionary<string, string> _mechanismLayers;

        [ViewVariables] public string ID => _id;

        [ViewVariables] public string Name => _name;

        [ViewVariables] public string CenterSlot => _centerSlot;

        [ViewVariables] public Dictionary<string, BodyPartType> Slots => new(_slots);

        [ViewVariables]
        [YamlField("connections", priority: 2)]
        public Dictionary<string, List<string>> Connections
        {
            get => _connections.ToDictionary(x => x.Key, x => x.Value.ToList());
            private set
            {
                //Our prototypes don't force the user to define a BodyPart connection twice. E.g. Head: Torso v.s. Torso: Head.
                //The user only has to do one. We want it to be that way in the code, though, so this cleans that up.
                var cleanedConnections = new Dictionary<string, List<string>>();
                foreach (var targetSlotName in _slots.Keys)
                {
                    var tempConnections = new List<string>();
                    foreach (var (slotName, slotConnections) in value)
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

        [ViewVariables] public Dictionary<string, string> Layers => new(_layers);

        [ViewVariables] public Dictionary<string, string> MechanismLayers => new(_mechanismLayers);
    }
}
