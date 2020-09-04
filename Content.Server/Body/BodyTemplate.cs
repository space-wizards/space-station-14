using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Body;
using Content.Shared.Body.Template;
using Content.Shared.GameObjects.Components.Body;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.Body
{
    /// <summary>
    ///     This class is a data capsule representing the standard format of a
    ///     <see cref="BodyManagerComponent"/>.
    ///     For instance, the "humanoid" BodyTemplate defines two arms, each
    ///     connected to a torso and so on.
    ///     Capable of loading data from a <see cref="BodyTemplatePrototype"/>.
    /// </summary>
    public class BodyTemplate
    {
        [ViewVariables] public bool Initialized { get; private set; }

        [ViewVariables] public string Name { get; private set; } = "";

        /// <summary>
        ///     The name of the center BodyPart. For humans, this is set to "torso".
        ///     Used in many calculations.
        /// </summary>
        [ViewVariables]
        public string CenterSlot { get; set; } = "";

        /// <summary>
        ///     Maps all parts on this template to its BodyPartType.
        ///     For instance, "right arm" is mapped to "BodyPartType.arm" on the humanoid
        ///     template.
        /// </summary>
        [ViewVariables]
        public Dictionary<string, BodyPartType> Slots { get; private set; } = new Dictionary<string, BodyPartType>();

        /// <summary>
        ///     Maps limb name to the list of their connections to other limbs.
        ///     For instance, on the humanoid template "torso" is mapped to a list
        ///     containing "right arm", "left arm", "left leg", and "right leg".
        ///     This is mapped both ways during runtime, but in the prototype only one
        ///     way has to be defined, i.e., "torso" to "left arm" will automatically
        ///     map "left arm" to "torso".
        /// </summary>
        [ViewVariables]
        public Dictionary<string, List<string>> Connections { get; private set; } = new Dictionary<string, List<string>>();

        [ViewVariables]
        public Dictionary<string, string> Layers { get; private set; } = new Dictionary<string, string>();

        [ViewVariables]
        public Dictionary<string, string> MechanismLayers { get; private set; } = new Dictionary<string, string>();

        public bool Equals(BodyTemplate other)
        {
            return GetHashCode() == other.GetHashCode();
        }

        /// <summary>
        ///     Checks if the given slot exists in this <see cref="BodyTemplate"/>.
        /// </summary>
        /// <returns>True if it does, false otherwise.</returns>
        public bool HasSlot(string slotName)
        {
            return Slots.Keys.Any(slot => slot == slotName);
        }

        /// <summary>
        ///     Calculates the hash code for this instance of <see cref="BodyTemplate"/>.
        ///     It does not matter in which order the Connections or Slots are defined.
        /// </summary>
        /// <returns>
        ///     An integer unique to this <see cref="BodyTemplate"/>'s layout.
        /// </returns>
        public override int GetHashCode()
        {
            var slotsHash = 0;
            var connectionsHash = 0;

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

            // One of the unit tests considers 0 to be an error, but it will be 0 if
            // the BodyTemplate is empty, so let's shift that up to 1.
            var hash = HashCode.Combine(
                CenterSlot.GetHashCode(),
                slotsHash,
                connectionsHash);

            if (hash == 0)
            {
                hash++;
            }

            return hash;
        }

        public virtual void Initialize(BodyTemplatePrototype prototype)
        {
            DebugTools.Assert(!Initialized, $"{nameof(BodyTemplate)} {Name} has already been initialized!");

            Name = prototype.Name;
            CenterSlot = prototype.CenterSlot;
            Slots = new Dictionary<string, BodyPartType>(prototype.Slots);
            Connections = new Dictionary<string, List<string>>(prototype.Connections);
            Layers = new Dictionary<string, string>(prototype.Layers);
            MechanismLayers = new Dictionary<string, string>(prototype.MechanismLayers);

            Initialized = true;
        }
    }
}
