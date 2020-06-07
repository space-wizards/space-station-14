using System;
using System.Collections.Generic;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.BodySystem {

    /// <summary>
    ///     This class is a data capsule representing the standard format of a body. For instance, the "humanoid" BodyTemplate
    ///     defines two arms, each connected to a torso and so on. Capable of loading data from a BodyTemplatePrototype.
    /// </summary>	
    public class BodyTemplate {

        private int _hash;
        private string _name;
        private string _centerSlot;
        private Dictionary<string, BodyPartType> _slots = new Dictionary<string, BodyPartType>();
        private Dictionary<string, List<string>> _connections = new Dictionary<string, List<string>>();

        [ViewVariables]
        public int Hash => _hash;

        [ViewVariables]
        public string Name => _name;

        /// <summary>
        ///     The name of the center BodyPart. For humans, this is set to "torso". Used in many calculations.
        /// </summary>					
        [ViewVariables]
        public string CenterSlot => _centerSlot;

        /// <summary>
        ///     Maps all parts on this template to its BodyPartType. For instance, "right arm" is mapped to "BodyPartType.arm" on the humanoid template.
        /// </summary>			
        [ViewVariables]
        public Dictionary<string, BodyPartType> Slots => _slots;

        /// <summary>
        ///     Maps limb name to the list of their connections to other limbs. For instance, on the humanoid template "torso" is mapped to a list containing "right arm", "left arm",
        ///     "left leg", and "right leg". Only one of the limbs in a connection has to map it, i.e. humanoid template chooses to map "head" to "torso" and not the other way around.
        /// </summary>			
        [ViewVariables]
        public Dictionary<string, List<string>> Connections => _connections;

        public BodyTemplate()
        {
            _name = "empty";
        }

        public BodyTemplate(BodyTemplatePrototype data)
        {
            LoadFromPrototype(data);
        }

        /// <summary>
        ///     Somewhat costly operation. Stores an integer unique to this exact BodyTemplate in _hash when called.
        /// </summary>		
        private void CacheHashCode()
        {
            int hash = 0;
            foreach (var(key, value) in _slots)
            {
                hash = HashCode.Combine<int, int>(hash, key.GetHashCode());
            }
            foreach (var (key, value) in _connections)
            {
                hash = HashCode.Combine<int, int>(hash, key.GetHashCode());
                foreach (var connection in value)
                {
                    hash = HashCode.Combine<int, int>(hash, connection.GetHashCode());
                }
            }
            _hash = hash;
        }

        public virtual void LoadFromPrototype(BodyTemplatePrototype data)
        {
            _name = data.Name;
            _centerSlot = data.CenterSlot;
            _slots = data.Slots;
            _connections = data.Connections;
            CacheHashCode();
        }
    }
}
