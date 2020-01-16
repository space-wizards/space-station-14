using System.Collections.Generic;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.BodySystem {
    /// <summary>
    ///     Stores data on what BodyParts should fill a BodyTemplate. Used for loading complete body presets, like a "basic human" with all human limbs.
    /// </summary>			
    [Prototype("bodyPreset")]
    public class BodyPreset : IPrototype, IIndexedPrototype {
        private string _id;
        private string _name;
		private string _templateID;
		private Dictionary<string,string> _partIDs;

        [ViewVariables]
        public string ID => _id;

        [ViewVariables]
        public string Name => _name;
		
        [ViewVariables]
        public string TemplateID => _templateID;

        /// <summary>
        ///     Maps a template slot to the BodyPart that should fill it. E.g. "right arm" : "BodyPart.arm.basic_human".
        /// </summary>		
        [ViewVariables]
		public Dictionary<string,string> PartIDs => _partIDs;

        public virtual void LoadFrom(YamlMappingNode mapping){
            var serializer = YamlObjectSerializer.NewReader(mapping);

            serializer.DataField(ref _name, "name", string.Empty);
            serializer.DataField(ref _id, "id", string.Empty);
            serializer.DataField(ref _templateID, "templateID", string.Empty);
			serializer.DataField(ref _partIDs, "partIDs", new Dictionary<string, string>());
        }
    }
}
