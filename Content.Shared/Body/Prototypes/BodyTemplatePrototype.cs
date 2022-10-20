using Content.Shared.Body.Part;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Body.Prototypes
{
    /// <summary>
    ///     Defines the layout of a body.
    /// </summary>
    [Prototype("bodyTemplate")]
    [Serializable, NetSerializable]
    public readonly record struct BodyTemplatePrototype : IPrototype
    {
        [IncludeDataField] private readonly ConnectionsData _connectionsData = default;

        [DataField("layers")] private readonly Dictionary<string, string> _layers = new();

        [DataField("mechanismLayers")] private readonly Dictionary<string, string> _mechanismLayers = new();

        [ViewVariables]
        [IdDataFieldAttribute]
        public string ID { get; } = default!;

        [ViewVariables]
        [DataField("name")]
        public string Name { get; } = string.Empty;

        [ViewVariables]
        [DataField("centerSlot")]
        public string CenterSlot { get; } = string.Empty;

        [ViewVariables]
        public Dictionary<string, BodyPartType> Slots => new(_connectionsData.Slots);

        [ViewVariables] public Dictionary<string, HashSet<string>> Connections => _connectionsData.Connections;

        [ViewVariables]
        public Dictionary<string, string> Layers => new(_layers);

        [ViewVariables]
        public Dictionary<string, string> MechanismLayers => new(_mechanismLayers);

        private struct ConnectionsData : ISerializationHooks
        {
            [DataField("slots")] public readonly Dictionary<string, BodyPartType> Slots = new();

            [DataField("connections")] private readonly Dictionary<string, List<string>> _rawConnections = new();

            public Dictionary<string, HashSet<string>> Connections = new();

            void ISerializationHooks.AfterDeserialization()
            {
                //Our prototypes don't force the user to define a BodyPart connection twice. E.g. Head: Torso v.s. Torso: Head.
                //The user only has to do one. We want it to be that way in the code, though, so this cleans that up.
                var cleanedConnections = new Dictionary<string, HashSet<string>>();

                foreach (var targetSlotName in Slots.Keys)
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
}
