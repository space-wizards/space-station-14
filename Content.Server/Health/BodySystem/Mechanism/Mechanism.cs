using System;
using System.Collections.Generic;
using Content.Shared.BodySystem;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;




namespace Content.Server.BodySystem {

    /// <summary>
    ///     Data class representing a persistent item inside a <see cref="BodyPart"/>. This includes livers, eyes, cameras, brains, explosive implants, binary communicators, and other things.
    /// </summary>
    public class Mechanism {

        [ViewVariables]
        public string Name { get; set; }

        /// <summary>
        ///     Professional description of the Mechanism.
        /// </summary>				
        [ViewVariables]
        public string Description { get; set; }

        /// <summary>
        ///     The message to display upon examining a mob with this Mechanism installed. If the string is empty (""), no message will be displayed.  
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

        public Mechanism(MechanismPrototype data)
        {
            LoadFromPrototype(data);
        }






        /// <summary>
        ///    Loads the given <see cref="MechanismPrototype"/> - current data on this Mechanism will be overwritten!
        /// </summary>	
        public void LoadFromPrototype(MechanismPrototype data)
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
        }
    }
}

