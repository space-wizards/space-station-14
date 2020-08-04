using System;
using System.Collections.Generic;
using Content.Server.Body.Mechanisms.Behaviors;
using Content.Shared.Body.Mechanism;
using Content.Shared.GameObjects.Components.Body;
using Robust.Shared.ViewVariables;

namespace Content.Server.Body.Mechanisms
{
    /// <summary>
    ///     Data class representing a persistent item inside a <see cref="BodyPart"/>. This includes livers, eyes, cameras,
    ///     brains, explosive implants, binary communicators, and other things.
    /// </summary>
    public class Mechanism
    {
        public Mechanism(MechanismPrototype data)
        {
            LoadFromPrototype(data);
        }

        [ViewVariables] public string Name { get; set; }

        /// <summary>
        ///     Professional description of the Mechanism.
        /// </summary>
        [ViewVariables]
        public string Description { get; set; }

        /// <summary>
        ///     The message to display upon examining a mob with this Mechanism installed. If the string is empty (""), no message
        ///     will be displayed.
        /// </summary>
        [ViewVariables]
        public string ExamineMessage { get; set; }

        /// <summary>
        ///     Path to the RSI that represents this Mechanism.
        /// </summary>
        [ViewVariables]
        public string RSIPath { get; set; }

        /// <summary>
        ///     RSI state that represents this Mechanism.
        /// </summary>
        [ViewVariables]
        public string RSIState { get; set; }

        /// <summary>
        ///     Max HP of this Mechanism.
        /// </summary>
        [ViewVariables]
        public int MaxDurability { get; set; }

        /// <summary>
        ///     Current HP of this Mechanism.
        /// </summary>
        [ViewVariables]
        public int CurrentDurability { get; set; }

        /// <summary>
        ///     At what HP this Mechanism is completely destroyed.
        /// </summary>
        [ViewVariables]
        public int DestroyThreshold { get; set; }

        /// <summary>
        ///     Armor of this Mechanism against attacks.
        /// </summary>
        [ViewVariables]
        public int Resistance { get; set; }

        /// <summary>
        ///     Determines a handful of things - mostly whether this Mechanism can fit into a BodyPart.
        /// </summary>
        [ViewVariables]
        public int Size { get; set; }

        /// <summary>
        ///     What kind of BodyParts this Mechanism can be easily installed into.
        /// </summary>
        [ViewVariables]
        public BodyPartCompatibility Compatibility { get; set; }

        /// <summary>
        ///     The behaviors that this mechanism performs.
        /// </summary>
        [ViewVariables]
        public List<MechanismBehavior> MechanismBehaviors { get; set; }

        /// <summary>
        ///     This method is called by <see cref="BodyPart.Update"/>
        /// </summary>
        public void Update(float frameTime)
        {
            foreach (var behavior in MechanismBehaviors)
            {
                behavior.Update(frameTime);
            }
        }

        /// <summary>
        ///     Loads the given <see cref="MechanismPrototype"/> - current data on this Mechanism will be overwritten!
        /// </summary>
        private void LoadFromPrototype(MechanismPrototype data)
        {
            Name = data.Name;
            Description = data.Description;
            ExamineMessage = data.ExamineMessage;
            RSIPath = data.RSIPath;
            RSIState = data.RSIState;
            MaxDurability = data.Durability;
            CurrentDurability = MaxDurability;
            DestroyThreshold = data.DestroyThreshold;
            Resistance = data.Resistance;
            Size = data.Size;
            Compatibility = data.Compatibility;
            MechanismBehaviors = new List<MechanismBehavior>();

            foreach (var mechanismBehaviorName in data.BehaviorClasses)
            {
                var mechanismBehaviorType = Type.GetType(mechanismBehaviorName);
                if (mechanismBehaviorType == null)
                {
                    throw new InvalidOperationException(
                        $"No {nameof(MechanismBehavior)} found with name {mechanismBehaviorName}");
                }

                if (!mechanismBehaviorType.IsSubclassOf(typeof(MechanismBehavior)))
                {
                    throw new InvalidOperationException(
                        $"Class {mechanismBehaviorName} is not a subtype of {nameof(MechanismBehavior)} for mechanism prototype {data.ID}");
                }

                var newBehavior = (MechanismBehavior) Activator.CreateInstance(mechanismBehaviorType, this);
                MechanismBehaviors.Add(newBehavior);
            }
        }
    }
}
