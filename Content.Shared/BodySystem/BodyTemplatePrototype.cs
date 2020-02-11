using System.Collections.Generic;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.BodySystem {

    /// <summary>
    ///     This class represents a standard format of a body. For instance, a "humanoid" has two arms, each connected to a torso and
    ///     so on. It is a prototype, so you can load templates from YAML.
    /// </summary>	
    [Prototype("bodyTemplate")]
    public class BodyTemplate : IPrototype, IIndexedPrototype {
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

        /// <summary>
        ///     Maps all parts on this template to its BodyPartType. For instance, "right arm" is mapped to "BodyPartType.arm" on the humanoid template.
        /// </summary>			
        [ViewVariables]
        public Dictionary<string, BodyPartType> Slots => _slots;

        /// <summary>
        ///     Maps limb name to the list of their connections. For instance on the humanoid template, "torso" is mapped to a list containing "right arm", "left arm",
        ///     "left leg", and "right leg". Only one of the limbs in a connection has to map it. e.g. humanoid template maps "head" to "torso" and not the other way around.
        /// </summary>			
        [ViewVariables]
        public Dictionary<string, List<string>> Connections => _connections;



        public virtual void LoadFrom(YamlMappingNode mapping){
            var serializer = YamlObjectSerializer.NewReader(mapping);
            serializer.DataField(ref _name, "name", string.Empty);
            serializer.DataField(ref _id, "id", string.Empty);
			serializer.DataField(ref _centerSlot, "centerSlot", string.Empty);
            serializer.DataField(ref _slots, "slots", new Dictionary<string, BodyPartType>());

            serializer.DataField(ref _connections, "connections", new Dictionary<string, List<string>>());
        }
    }
}
