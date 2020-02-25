using System;
using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;




namespace Content.Shared.BodySystem {

    /// <summary>
    ///     Data class representing a persistent item inside a BodyPart. This includes livers, eyes, cameras, brains, explosive implants, binary communicators, etc.
    /// </summary>
    [NetSerializable, Serializable]
    public class Mechanism {

        [ViewVariables]
        public string Name;
		
		/// <summary>
		///     Description shown in a mechanism installation console or when examining an uninstalled mechanism.
		/// </summary>				
		[ViewVariables]
        public string Description;		
		
		/// <summary>
		///     The message to display upon examining a mob with this mechanism installed. If the string is empty (""), no message will be displayed.  
		/// </summary>			  
		[ViewVariables]
		public string ExamineMessage;

        /// <summary>
        ///     Path to the .png that represents this mechanism (NOT .rsi folder).
        /// </summary>			  
        [ViewVariables]
        public string SpritePath;

        /// <summary>
        ///     Max HP of this mechanism.
        /// </summary>		
		[ViewVariables]
        public int MaxDurability;

        /// <summary>
        ///     Current HP of this mechanism.
        /// </summary>		
        [ViewVariables]
        public int CurrentDurability;
		
		/// <summary>
        ///     At what HP this mechanism is completely destroyed.
        /// </summary>		
		[ViewVariables]
		public int DestroyThreshold;	
		
        /// <summary>
        ///     Armor of this mechanism against attacks.
        /// </summary>		
		[ViewVariables]
		public int Resistance;
		
        /// <summary>
        ///     Determines a handful of things - mostly whether this mechanism can fit into a BodyPart.
        /// </summary>		
		[ViewVariables]
		public int Size;

        /// <summary>
        ///     What kind of BodyParts this mechanism can be installed into.
        /// </summary>
        [ViewVariables]
        public BodyPartCompatibility Compatibility;

        public Mechanism(MechanismPrototype data)
        {
            LoadFromPrototype(data);
        }

        /// <summary>
        ///    Loads the given MechanismPrototype - current data on this Mechanism will be overwritten!
        /// </summary>	
        public void LoadFromPrototype(MechanismPrototype data)
        {
            Name = data.Name;
            Description = data.Description;
            ExamineMessage = data.ExamineMessage;
            SpritePath = data.SpritePath;
            MaxDurability = data.Durability;
            CurrentDurability = MaxDurability;
            DestroyThreshold = data.DestroyThreshold;
            Resistance = data.Resistance;
            Size = data.Size;
            Compatibility = data.Compatibility;
        }
    }
}

