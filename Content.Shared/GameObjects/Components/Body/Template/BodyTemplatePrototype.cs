#nullable enable
using System;
using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Body.Part;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Body.Template
{
    /// <summary>
    ///     Defines the layout of a <see cref="IBody"/>.
    /// </summary>
    [Prototype("bodyTemplate")]
    [Serializable, NetSerializable]
    public class BodyTemplatePrototype : IPrototype, ISerializationHooks
    {
        [DataField("slots")]
        private Dictionary<string, BodyPartType> _slots = new();

        [DataField("connections")]
        private Dictionary<string, List<string>> _rawConnections = new();

        [DataField("layers")]
        private Dictionary<string, string> _layers = new();

        [DataField("mechanismLayers")]
        private Dictionary<string, string> _mechanismLayers = new();

        [ViewVariables]
        [DataField("id", required: true)]
        public string ID { get; } = default!;

        [ViewVariables]
        [DataField("name")]
        public string Name { get; } = string.Empty;

        [ViewVariables]
        [DataField("centerSlot")]
        public string CenterSlot { get; } = string.Empty;

        [ViewVariables]
        public Dictionary<string, BodyPartType> Slots => new(_slots);

        [ViewVariables]
        public Dictionary<string, HashSet<string>> Connections { get; set; } = new();

        [ViewVariables]
        public Dictionary<string, string> Layers => new(_layers);

        [ViewVariables]
        public Dictionary<string, string> MechanismLayers => new(_mechanismLayers);

        void ISerializationHooks.AfterDeserialization()
        {
            //Our prototypes don't force the user to define a BodyPart connection twice. E.g. Head: Torso v.s. Torso: Head.
            //The user only has to do one. We want it to be that way in the code, though, so this cleans that up.
            var cleanedConnections = new Dictionary<string, HashSet<string>>();

            foreach (var targetSlotName in _slots.Keys)
            {
                var tempConnections = new HashSet<string>();
                foreach (var (slotName, slotConnections) in _rawConnections)
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

            Connections = cleanedConnections;
        }
    }
}
