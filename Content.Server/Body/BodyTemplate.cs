using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Body;
using Content.Shared.Body.Template;
using Content.Shared.GameObjects.Components.Body;
using Robust.Shared.ViewVariables;

namespace Content.Server.Body
{
    /// <summary>
    ///     This class is a data capsule representing the standard format of a
    ///     <see cref="BodyManagerComponent"/>.
    ///     For instance, the "humanoid" BodyTemplate defines two arms, each connected to
    ///     a torso and so on.
    ///     Capable of loading data from a <see cref="BodyTemplatePrototype"/>.
    /// </summary>
    public class BodyTemplate
    {
        public BodyTemplate()
        {
            Name = "empty";
            CenterSlot = "";
            Slots = new Dictionary<string, BodyPartType>();
            Connections = new Dictionary<string, List<string>>();
            Networks = new List<string>();
        }

        public BodyTemplate(BodyTemplatePrototype data)
        {
            LoadFromPrototype(data);
        }

        [ViewVariables] public string Name { get; private set; }

        /// <summary>
        ///     The name of the center BodyPart. For humans, this is set to "torso".
        ///     Used in many calculations.
        /// </summary>
        [ViewVariables]
        public string CenterSlot { get; set; }

        /// <summary>
        ///     Maps all parts on this template to its BodyPartType.
        ///     For instance, "right arm" is mapped to "BodyPartType.arm" on the humanoid
        ///     template.
        /// </summary>
        [ViewVariables]
        public Dictionary<string, BodyPartType> Slots { get; private set; }

        /// <summary>
        ///     Maps limb name to the list of their connections to other limbs.
        ///     For instance, on the humanoid template "torso" is mapped to a list
        ///     containing "right arm", "left arm", "left leg", and "right leg".
        ///     This is mapped both ways during runtime, but in the prototype only one
        ///     way has to be defined, i.e., "torso" to "left arm" will automatically
        ///     map "left arm" to "torso".
        /// </summary>
        [ViewVariables]
        public Dictionary<string, List<string>> Connections { get; private set; }

        /// <summary>
        ///     Lists all of the networks that this template possesses.
        /// </summary>
        [ViewVariables]
        public List<string> Networks { get; private set; }

        public bool Equals(BodyTemplate other)
        {
            return GetHashCode() == other.GetHashCode();
        }

        /// <summary>
        ///     Returns whether the given slot exists in this BodyTemplate.
        /// </summary>
        public bool SlotExists(string slotName)
        {
            return Slots.Keys.Any(slot => slot == slotName);
        }

        /// <summary>
        ///     Returns an integer unique to this BodyTemplate's layout.
        ///     It does not matter in which order the Connections or Slots are defined.
        /// </summary>
        public override int GetHashCode()
        {
            var slotsHash = 0;
            var connectionsHash = 0;
            var networksHash = 0;

            foreach (var (key, value) in Slots)
            {
                var slot = key.GetHashCode();
                slot = HashCode.Combine(slot, value.GetHashCode());
                slotsHash ^= slot;
            }

            var connections = new List<int>();
            foreach (var (key, value) in Connections)
            {
                foreach (var targetBodyPart in value)
                {
                    var connection = key.GetHashCode() ^ targetBodyPart.GetHashCode();
                    if (!connections.Contains(connection))
                    {
                        connections.Add(connection);
                    }
                }
            }

            foreach (var connection in connections)
            {
                connectionsHash ^= connection;
            }

            foreach (var network in Networks)
            {
                networksHash ^= network.GetHashCode();
            }

            // One of the unit tests considers 0 to be an error, but it will be 0 if the BodyTemplate is empty, so let's shift that up to 1.
            var hash = HashCode.Combine(
                CenterSlot.GetHashCode(),
                slotsHash,
                connectionsHash,
                networksHash);

            if (hash == 0)
            {
                hash++;
            }

            return hash;
        }

        protected virtual void LoadFromPrototype(BodyTemplatePrototype data)
        {
            Name = data.Name;
            CenterSlot = data.CenterSlot;
            Slots = data.Slots;
            Connections = data.Connections;
            Networks = data.Networks;
        }
    }
}
