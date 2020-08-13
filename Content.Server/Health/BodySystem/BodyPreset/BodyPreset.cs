using System.Collections.Generic;
using Content.Server.Health.BodySystem.BodyPart;
using Content.Shared.Health.BodySystem.BodyPart;
using Content.Shared.Health.BodySystem.BodyPreset;
using Robust.Shared.ViewVariables;

namespace Content.Server.Health.BodySystem.BodyPreset {

    /// <summary>
    ///     Stores data on what <see cref="BodyPartPrototype">BodyPartPrototypes</see> should fill a BodyTemplate. Used for loading complete body presets, like a "basic human" with all human limbs.
    /// </summary>
    public class BodyPreset {
        private string _name;
		private Dictionary<string,string> _partIDs;

        [ViewVariables]
        public string Name => _name;

        /// <summary>
        ///     Maps a template slot to the ID of the <see cref="BodyPart"> that should fill it. E.g. "right arm" : "BodyPart.arm.basic_human".
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
