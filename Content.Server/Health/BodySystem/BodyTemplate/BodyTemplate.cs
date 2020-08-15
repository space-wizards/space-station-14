using System;
using System.Collections.Generic;
using Content.Shared.Health.BodySystem;
using Content.Shared.Health.BodySystem.BodyTemplate;
using Robust.Shared.ViewVariables;

namespace Content.Server.Health.BodySystem.BodyTemplate {

    /// <summary>
    ///     This class is a data capsule representing the standard format of a <see cref="BodyManagerComponent"/>. For instance, the "humanoid" BodyTemplate
    ///     defines two arms, each connected to a torso and so on. Capable of loading data from a <see cref="BodyTemplatePrototype"/>.
    /// </summary>
    public class BodyTemplate {

        [ViewVariables]
        public string Name;

        /// <summary>
        ///     The name of the center BodyPart. For humans, this is set to "torso". Used in many calculations.
        /// </summary>
        [ViewVariables]
        public string CenterSlot { get; set; }

        /// <summary>
        ///     Maps all parts on this template to its BodyPartType. For instance, "right arm" is mapped to "BodyPartType.arm" on the humanoid template.
        /// </summary>
        [ViewVariables]
        public Dictionary<string, BodyPartType> Slots { get; set; }

        /// <summary>
        ///     Maps limb name to the list of their connections to other limbs. For instance, on the humanoid template "torso" is mapped to a list containing "right arm", "left arm",
        ///     "left leg", and "right leg". This is mapped both ways during runtime, but in the prototype only one way has to be defined, i.e., "torso" to "left arm" will automatically
        ///     map "left arm" to "torso".
        /// </summary>
        [ViewVariables]
        public Dictionary<string, List<string>> Connections { get; set; }

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
            return GetHashCode() == other.GetHashCode();
        }

        /// <summary>
        ///     Returns whether the given slot exists in this BodyTemplate.
        /// </summary>
        public bool SlotExists(string slotName)
        {
            foreach (string slot in Slots.Keys)
            {
                if (slot == slotName) //string comparison xd
                    return true;
            }
            return false;
        }

        /// <summary>
        ///     Returns an integer unique to this BodyTemplate's layout. It does not matter in which order the Connections or Slots are defined.
        /// </summary>
        public override int GetHashCode()
        {
            int slotsHash = 0;
            int connectionsHash = 0;

            foreach (var(key, value) in Slots)
            {
                int slot = key.GetHashCode();
                slot = HashCode.Combine<int, int>(slot, value.GetHashCode());
                slotsHash = slotsHash ^ slot;
            }

            List<int> connections = new List<int>();
            foreach (var (key, value) in Connections)
            {
                foreach (var targetBodyPart in value)
                {
                    int connection = key.GetHashCode() ^ targetBodyPart.GetHashCode();
                    if (!connections.Contains(connection))
                        connections.Add(connection);
                }
            }
            foreach (int connection in connections)
            {
                connectionsHash = connectionsHash ^ connection;
            }

            int hash = HashCode.Combine<int, int, int>(slotsHash, connectionsHash, CenterSlot.GetHashCode());
            if (hash == 0) //One of the unit tests considers 0 to be an error, but it will be 0 if the BodyTemplate is empty, so let's shift that up to 1.
                hash++;
            return hash;
        }

        public virtual void LoadFromPrototype(BodyTemplatePrototype data)
        {
            Name = data.Name;
            CenterSlot = data.CenterSlot;
            Slots = data.Slots;
            Connections = data.Connections;
        }
    }
}
