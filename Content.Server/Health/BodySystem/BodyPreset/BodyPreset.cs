using System;
using System.Collections.Generic;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.BodySystem {

    /// <summary>
    ///     Stores data on what BodyPart(Prototypes) should fill a BodyTemplate. Used for loading complete body presets, like a "basic human" with all human limbs.
    /// </summary>
    public class BodyPreset {
        private string _name;
		private Dictionary<string,string> _partIDs;

        [ViewVariables]
        public string Name => _name;

        /// <summary>
        ///     Maps a template slot to the ID of the BodyPart that should fill it. E.g. "right arm" : "BodyPart.arm.basic_human".
        /// </summary>		
        [ViewVariables]
		public Dictionary<string, string> PartIDs => _partIDs;

        public BodyPreset(BodyPresetPrototype data)
        {
            LoadFromPrototype(data);
        }

        public virtual void LoadFromPrototype(BodyPresetPrototype data)
        {
            _name = data.Name;
            _partIDs = data.PartIDs;
        }
    }
}
