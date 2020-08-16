using System;
using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Health.BodySystem.BodyTemplate {

    /// <summary>
    ///    Prototype for the BodyTemplate class.
    /// </summary>
    [Prototype("bodyTemplate")]
    [NetSerializable, Serializable]
    public class BodyTemplatePrototype : IPrototype, IIndexedPrototype {
        private string _id;
        private string _name;
		private string _centerSlot;
		private Dictionary<string, BodyPartType> _slots;
		private Dictionary<string, List<string>> _connections;

        [ViewVariables]
        public string ID => _id;

        [ViewVariables]
        public string Name => _name;

        [ViewVariables]
        public string CenterSlot => _centerSlot;

        [ViewVariables]
        public Dictionary<string, BodyPartType> Slots => _slots;

        [ViewVariables]
        public Dictionary<string, List<string>> Connections => _connections;

        public virtual void LoadFrom(YamlMappingNode mapping){
            var serializer = YamlObjectSerializer.NewReader(mapping);
            serializer.DataField(ref _name, "name", string.Empty);
            serializer.DataField(ref _id, "id", string.Empty);
			serializer.DataField(ref _centerSlot, "centerSlot", string.Empty);
            serializer.DataField(ref _slots, "slots", new Dictionary<string, BodyPartType>());
            serializer.DataField(ref _connections, "connections", new Dictionary<string, List<string>>());


            //Our prototypes don't force the user to define a BodyPart connection twice. E.g. Head: Torso v.s. Torso: Head.
            //The user only has to do one. We want it to be that way in the code, though, so this cleans that up.
            Dictionary<string, List<string>> cleanedConnections = new Dictionary<string, List<string>>();
            foreach (var (targetSlotName, slotType) in _slots)
            {
                List<string> tempConnections = new List<string>();
                foreach (var (slotName, slotConnections) in _connections)
                {
                    if (slotName == targetSlotName){
                        foreach (string connection in slotConnections) {
                            if (!tempConnections.Contains(connection))
                                tempConnections.Add(connection);
                        }
                    }
                    else if (slotConnections.Contains(targetSlotName))
                    {
                        tempConnections.Add(slotName);
                    }
                }
                if(tempConnections.Count > 0)
                    cleanedConnections.Add(targetSlotName, tempConnections);
            }
            _connections = cleanedConnections;
        }
    }
}
