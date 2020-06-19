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

        /// <summary>
        ///     Integer unique to this BodyTemplate's layout. It does not matter in which order the Connections or Slots are defined.
        /// </summary>	
        [ViewVariables]
        public int Hash
        {
            get
            {
                if (_hash == 0)
                    CacheHashCode();
                return _hash;
            }
        }

        [ViewVariables]
        public string Name;

        /// <summary>
        ///     The name of the center BodyPart. For humans, this is set to "torso". Used in many calculations.
        /// </summary>					
        [ViewVariables]
        public string CenterSlot
        {
            get
            {
                return CenterSlot;
            }
            set
            {
                CenterSlot = value;
                CacheHashCode();
            }
        }

        /// <summary>
        ///     Maps all parts on this template to its BodyPartType. For instance, "right arm" is mapped to "BodyPartType.arm" on the humanoid template.
        /// </summary>			
        [ViewVariables]
        public Dictionary<string, BodyPartType> Slots {
            get
            {
                return Slots;
            }
            set
            {
                Slots = value;
                CacheHashCode();
            }
        }

        /// <summary>
        ///     Maps limb name to the list of their connections to other limbs. For instance, on the humanoid template "torso" is mapped to a list containing "right arm", "left arm",
        ///     "left leg", and "right leg". Only one of the limbs in a connection has to map it, i.e. humanoid template chooses to map "head" to "torso" and not the other way around.
        /// </summary>			
        [ViewVariables]
        public Dictionary<string, List<string>> Connections
        {
            get
            {
                return Connections;
            }
            set
            {
                Connections = value;
                CacheHashCode();
            }
        }

        public BodyTemplate()
        {
            Name = "empty";
            Slots = new Dictionary<string, BodyPartType>();
            Connections = new Dictionary<string, List<string>>();
            CenterSlot = "";
        }

        public BodyTemplate(BodyTemplatePrototype data)
        {
            LoadFromPrototype(data);
        }

        public bool Equals(BodyTemplate other)
        {
            return Hash == other.Hash;
        }

        /// <summary>
        ///     Stores an integer unique to this BodyTemplate in _hash when called. Only considers the body itself, not things like name. 
        /// </summary>		
        private void CacheHashCode()
        {
            int slotsHash = 1;
            foreach (var(key, value) in Slots)
            {
                slotsHash = HashCode.Combine<int, int>(slotsHash, key.GetHashCode());
                slotsHash = HashCode.Combine<int, int>(slotsHash, value.GetHashCode());
            }
            foreach (var (key, value) in Connections)
            {
                slotsHash = HashCode.Combine<int, int>(slotsHash, key.GetHashCode());
                foreach (var connection in value)
                {
                    slotsHash = HashCode.Combine<int, int>(slotsHash, connection.GetHashCode());
                }
            }
            slotsHash = HashCode.Combine<int, int>(slotsHash, CenterSlot.GetHashCode());
            if (_hash == 0)
                _hash++;
            _hash = slotsHash;
        }

        public virtual void LoadFromPrototype(BodyTemplatePrototype data)
        {
            Name = data.Name;
            CenterSlot = data.CenterSlot;
            Slots = data.Slots;
            Connections = data.Connections;
            CacheHashCode();
        }
    }
}
